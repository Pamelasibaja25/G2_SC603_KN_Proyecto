using G2_SC603_KN_Proyecto.Models;
using G2_SC603_KN_Proyecto.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G2_SC603_KN_Proyecto.Controllers
{
    public class MembresiaController : Controller
    {
        private readonly DbOrionFitContext _context;

        public MembresiaController(DbOrionFitContext context)
        {
            _context = context;
        }

        #region Mostrar Membresia
        public async Task<IActionResult> MostrarMembresia()
        {
            List<ClienteMembresiaResumen> clientes = await _context.ClienteMembresiaResumen
                .FromSqlRaw("CALL sp_obtenerClientesMembresias()")
                .ToListAsync();

            // El SP no trae el monto realmente pagado (solo el precio fijo
            // del plan); se completa aparte con el último pago verificado
            // de cada cliente, para no confundir "precio del plan" con
            // "lo que efectivamente cobró" cuando paga varios meses juntos.
            var idsClientes = clientes.Where(c => c.IdCliente.HasValue).Select(c => c.IdCliente!.Value).ToList();
            var ultimosPagos = await _context.Pagos
                .Include(p => p.IdClienteMembresiaNavigation)
                .Where(p => p.EstadoVerificacion == "Verificado"
                    && idsClientes.Contains(p.IdClienteMembresiaNavigation.IdCliente))
                .GroupBy(p => p.IdClienteMembresiaNavigation.IdCliente)
                .Select(g => new { IdCliente = g.Key, Monto = g.OrderByDescending(p => p.FechaPago).First().Monto })
                .ToDictionaryAsync(x => x.IdCliente, x => x.Monto);

            foreach (var c in clientes)
            {
                if (c.IdCliente.HasValue && ultimosPagos.TryGetValue(c.IdCliente.Value, out decimal monto))
                {
                    c.MontoPagado = monto;
                }
            }

            var listaclientes = _context.Clientes.ToList();
            var listamembresias = _context.Membresia.ToList();

            ViewBag.Clientes = listaclientes;
            ViewBag.Membresias = listamembresias;
            // El negocio maneja una sola modalidad (mensualidad); planes viejos por datos históricos no se ofrecen como opción nueva
            ViewBag.MembresiaUnica = listamembresias.OrderBy(m => m.DuracionDias).FirstOrDefault();

            return View(clientes);
        }
        #endregion

        #region Agregar Membresia
        [HttpPost]
        [RolAutorizado("ADMIN", "RECEPTION")]
        public async Task<IActionResult> AgregarMembresia(ClienteMembresiaResumen nuevoCliente)
        {
            try
            {
                if (nuevoCliente.IdMembresia <= 0)
                {
                    TempData["ErrorMessage"] = "Primero configurá el monto de la mensualidad (botón \"Configurar Monto\").";
                    return RedirectToAction("MostrarMembresia");
                }

                await _context.Database.ExecuteSqlRawAsync(
                    "CALL sp_agregarClienteMembresia({0}, {1}, {2}, {3}, {4})",
                    nuevoCliente.IdCliente,
                    nuevoCliente.IdMembresia,
                    nuevoCliente.FechaInicio,
                    nuevoCliente.FechaFin,
                    nuevoCliente.Estado
                );

                // Si el admin la marcó como "Activa" es porque ya cobró el
                // período completo (1, 3, 6, 12 meses...) — se registra el
                // pago real para que Ingresos y el historial del cliente
                // reflejen la plata que efectivamente entró, no solo el
                // precio de un mes.
                if (nuevoCliente.Estado == "Activa")
                {
                    int meses = nuevoCliente.Meses.GetValueOrDefault(1);
                    if (meses < 1) meses = 1;

                    var membresiaCreada = await _context.ClienteMembresia
                        .Include(cm => cm.IdMembresiaNavigation)
                        .Where(cm => cm.IdCliente == nuevoCliente.IdCliente)
                        .OrderByDescending(cm => cm.IdClienteMembresia)
                        .FirstOrDefaultAsync();

                    if (membresiaCreada != null)
                    {
                        decimal precioMensual = membresiaCreada.IdMembresiaNavigation?.Precio ?? 0;

                        _context.Pagos.Add(new Pago
                        {
                            IdClienteMembresia = membresiaCreada.IdClienteMembresia,
                            Monto = precioMensual * meses,
                            FechaPago = DateOnly.FromDateTime(DateTime.Today),
                            MetodoPago = "Efectivo",
                            Descripcion = meses > 1
                                ? $"Pago adelantado registrado por el admin ({meses} meses)."
                                : "Pago registrado directamente por el admin.",
                            EstadoVerificacion = "Verificado"
                        });
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["SuccessMessage"] = "Cliente agregado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al agregar el cliente: " + ex.Message;
            }

            return RedirectToAction("MostrarMembresia");
        }
        #endregion

        #region Editar Membresía
        [HttpPost]
        [RolAutorizado("ADMIN", "RECEPTION")]
        public async Task<IActionResult> EditarMembresia(ClienteMembresiaResumen clienteEditado)
        {
            if (clienteEditado.FechaFin.HasValue && clienteEditado.FechaInicio.HasValue
                && clienteEditado.FechaFin < clienteEditado.FechaInicio)
            {
                TempData["ErrorMessage"] = "La fecha de vencimiento no puede ser anterior a la fecha de inicio.";
                return RedirectToAction("MostrarMembresia");
            }

            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "CALL sp_actualizarClienteMembresia({0}, {1}, {2}, {3}, {4})",
                    clienteEditado.IdCliente,
                    clienteEditado.IdMembresia,
                    clienteEditado.FechaInicio,
                    clienteEditado.FechaFin,
                    clienteEditado.Estado
                );
                TempData["SuccessMessage"] = "Cliente actualizado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al editar el cliente: " + ex.Message;
            }

            return RedirectToAction("MostrarMembresia");
        }
        #endregion

        #region Mostrar Historial
        // La tabla historial_membresias nunca se llena (el SP de crear
        // mensualidad no escribe ahí), así que en vez de depender de eso
        // mostramos el historial de pagos real del cliente.
        public async Task<IActionResult> ObtenerHistorial(int idCliente)
        {
            var historial = await _context.Pagos
                .Include(p => p.IdClienteMembresiaNavigation)
                .Where(p => p.IdClienteMembresiaNavigation.IdCliente == idCliente)
                .OrderByDescending(p => p.FechaPago)
                .Select(p => new
                {
                    fecha = p.FechaPago,
                    monto = p.Monto,
                    metodo = p.MetodoPago,
                    estado = p.EstadoVerificacion
                })
                .ToListAsync();

            return Json(historial);
        }
        #endregion

        #region Mostrar Membresías a Vencer
        public async Task<IActionResult> ObtenerMembresiasProximas()
        {
            var lista = await _context.MembresiasProximasVencer
                .FromSqlRaw("CALL sp_obtenerMembresiasProximasVencer()")
                .ToListAsync();

            return Json(lista);
        }
        #endregion
        #region Configurar Monto de la Mensualidad
        [HttpPost]
        [RolAutorizado("ADMIN")]
        public async Task<IActionResult> EditarMontoMensualidad(int idMembresia, decimal precio)
        {
            try
            {
                if (precio <= 0)
                {
                    TempData["ErrorMessage"] = "El monto debe ser mayor a cero.";
                    return RedirectToAction("MostrarMembresia");
                }

                Membresium? membresia = idMembresia > 0
                    ? await _context.Membresia.FirstOrDefaultAsync(m => m.IdMembresia == idMembresia)
                    : null;

                if (membresia == null)
                {
                    // Todavía no existe ninguna mensualidad configurada: se crea la única.
                    membresia = new Membresium
                    {
                        Nombre = "Mensualidad",
                        Precio = precio,
                        DuracionDias = 30
                    };
                    _context.Membresia.Add(membresia);
                    TempData["SuccessMessage"] = "Mensualidad creada y monto configurado correctamente.";
                }
                else
                {
                    membresia.Precio = precio;
                    TempData["SuccessMessage"] = "Monto de la mensualidad actualizado correctamente.";
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al actualizar el monto: " + ex.Message;
            }

            return RedirectToAction("MostrarMembresia");
        }
        #endregion
    }
}
