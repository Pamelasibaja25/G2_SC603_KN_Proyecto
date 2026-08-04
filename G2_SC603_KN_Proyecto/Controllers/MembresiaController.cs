using G2_SC603_KN_Proyecto.Models;
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

            var listaclientes = _context.Clientes.ToList();
            var listamembresias = _context.Membresia.ToList();

            ViewBag.Clientes = listaclientes;
            ViewBag.Membresias = listamembresias;
            // El negocio maneja una sola modalidad: la mensualidad (menor duración).
            // Si existen planes viejos (trimestral, etc.) en la BD por datos históricos,
            // no se muestran como opción nueva, solo se usa la mensual.
            ViewBag.MembresiaUnica = listamembresias.OrderBy(m => m.DuracionDias).FirstOrDefault();

            return View(clientes);
        }
        #endregion

        #region Agregar Membresia
        [HttpPost]
        public async Task<IActionResult> AgregarMembresia(ClienteMembresiaResumen nuevoCliente)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "CALL sp_agregarClienteMembresia({0}, {1}, {2}, {3}, {4})",
                    nuevoCliente.IdCliente,
                    nuevoCliente.IdMembresia,
                    nuevoCliente.FechaInicio,
                    nuevoCliente.FechaFin,
                    nuevoCliente.Estado
                );
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
        public async Task<IActionResult> EditarMembresia(ClienteMembresiaResumen clienteEditado)
        {
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
        public async Task<IActionResult> ObtenerHistorial(int idCliente)
        {
            var historial = await _context.HistorialMembresias
                .FromSqlRaw("CALL sp_obtenerHistorialMembresia({0})", idCliente)
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
        public async Task<IActionResult> EditarMontoMensualidad(int idMembresia, decimal precio)
        {
            try
            {
                Membresium? membresia = await _context.Membresia
                    .FirstOrDefaultAsync(m => m.IdMembresia == idMembresia);

                if (membresia == null)
                {
                    TempData["ErrorMessage"] = "No se encontró la mensualidad a modificar.";
                    return RedirectToAction("MostrarMembresia");
                }

                membresia.Precio = precio;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Monto de la mensualidad actualizado correctamente.";
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
