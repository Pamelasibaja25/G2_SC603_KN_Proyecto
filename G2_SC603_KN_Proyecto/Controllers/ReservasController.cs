using G2_SC603_KN_Proyecto.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G2_SC603_KN_Proyecto.Controllers
{
    // La reserva es la confirmación de asistencia al WOD (no un sistema de clases con horario/cupo)
    public class ReservasController : Controller
    {
        private readonly DbOrionFitContext _context;

        public ReservasController(DbOrionFitContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? filtroEstado = null)
        {
            string rol = HttpContext.Session.GetString("Rol") ?? string.Empty;
            int? idUsuario = HttpContext.Session.GetInt32("ID");

            Cliente? cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.IdUsuario == idUsuario);

            ReservasViewModel model = new ReservasViewModel();
            model.EsAdmin = rol.Contains("ADMIN") || rol.Contains("RECEPTION");
            model.EsCliente = cliente != null;

            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);

            if (cliente != null)
            {
                await PonerAlDiaConElUltimoWod(cliente.IdCliente, hoy);

                model.MisConfirmaciones = await _context.ClienteRutinas
                    .Include(cr => cr.IdRutinaNavigation)
                    .Where(cr => cr.IdCliente == cliente.IdCliente)
                    .OrderByDescending(cr => cr.FechaAsignacion)
                    .Select(cr => new ConfirmacionWodVM
                    {
                        IdClienteRutina = cr.IdClienteRutina,
                        IdRutina = cr.IdRutina,
                        NombreWod = cr.IdRutinaNavigation.Nombre,
                        Imagen = cr.IdRutinaNavigation.Imagen,
                        Fecha = cr.FechaAsignacion,
                        Estado = cr.EstadoAsistencia
                    })
                    .ToListAsync();
            }

            if (model.EsAdmin)
            {
                // Confirmados para hoy (publicado ayer)
                var confirmadosHoy = await _context.ClienteRutinas
                    .Include(cr => cr.IdRutinaNavigation)
                    .Include(cr => cr.IdClienteNavigation)
                    .Where(cr => cr.FechaAsignacion == hoy && cr.EstadoAsistencia == "ACEPTADO"
                        && cr.IdClienteNavigation.Estado == "Activo")
                    .OrderBy(cr => cr.IdClienteNavigation.Nombre)
                    .ToListAsync();

                var idsClientesHoy = confirmadosHoy.Select(cr => cr.IdCliente).Distinct().ToList();
                var yaAsistieron = await _context.Asistencia
                    .Where(a => a.Fecha == hoy && idsClientesHoy.Contains(a.IdCliente))
                    .Select(a => a.IdCliente)
                    .ToListAsync();

                model.ConfirmadosHoy = confirmadosHoy.Select(cr => new ConfirmacionWodVM
                {
                    IdClienteRutina = cr.IdClienteRutina,
                    IdRutina = cr.IdRutina,
                    NombreWod = cr.IdRutinaNavigation.Nombre,
                    NombreCliente = cr.IdClienteNavigation.Nombre,
                    Fecha = cr.FechaAsignacion,
                    Estado = cr.EstadoAsistencia,
                    AsistioHoy = yaAsistieron.Contains(cr.IdCliente)
                }).ToList();

                IQueryable<ClienteRutina> query = _context.ClienteRutinas
                    .Include(cr => cr.IdRutinaNavigation)
                    .Include(cr => cr.IdClienteNavigation);

                if (filtroEstado == "ACEPTADO" || filtroEstado == "NO_ASISTE" || filtroEstado == "PENDIENTE")
                {
                    query = query.Where(cr => cr.EstadoAsistencia == filtroEstado);
                }

                model.TodasConfirmaciones = await query
                    .OrderByDescending(cr => cr.FechaAsignacion)
                    .Select(cr => new ConfirmacionWodVM
                    {
                        IdClienteRutina = cr.IdClienteRutina,
                        IdRutina = cr.IdRutina,
                        NombreWod = cr.IdRutinaNavigation.Nombre,
                        NombreCliente = cr.IdClienteNavigation.Nombre,
                        Fecha = cr.FechaAsignacion,
                        Estado = cr.EstadoAsistencia
                    })
                    .ToListAsync();

                ViewBag.FiltroEstado = filtroEstado ?? "Todas";

                // Calendario: hoy + próximos 6 días
                DateOnly finVentana = hoy.AddDays(6);
                var confirmadosVentana = await _context.ClienteRutinas
                    .Include(cr => cr.IdRutinaNavigation)
                    .Include(cr => cr.IdClienteNavigation)
                    .Where(cr => cr.FechaAsignacion >= hoy && cr.FechaAsignacion <= finVentana
                        && cr.EstadoAsistencia == "ACEPTADO"
                        && cr.IdClienteNavigation.Estado == "Activo")
                    .ToListAsync();

                model.Calendario = Enumerable.Range(0, 7)
                    .Select(i =>
                    {
                        DateOnly fecha = hoy.AddDays(i);
                        var delDia = confirmadosVentana.Where(cr => cr.FechaAsignacion == fecha).ToList();
                        List<string> wods = delDia.Select(cr => cr.IdRutinaNavigation.Nombre).Distinct().ToList();

                        List<ClienteConfirmadoVM> clientes = delDia
                            .GroupBy(cr => new { cr.IdCliente, Nombre = cr.IdClienteNavigation.Nombre })
                            .Select(cg => new ClienteConfirmadoVM
                            {
                                Nombre = cg.Key.Nombre,
                                YaIngreso = fecha == hoy && _context.Asistencia.Any(a =>
                                    a.IdCliente == cg.Key.IdCliente && a.Fecha == hoy),
                                ConfirmoPorWod = wods.Select(w => cg.Any(x => x.IdRutinaNavigation.Nombre == w)).ToList()
                            })
                            .OrderBy(c => c.Nombre)
                            .ToList();

                        return new ConfirmadosDiaVM
                        {
                            Fecha = fecha,
                            Wods = wods,
                            Clientes = clientes
                        };
                    })
                    .ToList();
            }

            return View(model);
        }

        // Admin marca el check-in físico
        [HttpPost]
        public async Task<IActionResult> RegistrarAsistencia(int idClienteRutina)
        {
            string rol = HttpContext.Session.GetString("Rol") ?? string.Empty;
            if (!rol.Contains("ADMIN") && !rol.Contains("RECEPTION"))
            {
                TempData["ErrorMessage"] = "No tiene permisos para esta acción.";
                return RedirectToAction("Index");
            }

            ClienteRutina? confirmacion = await _context.ClienteRutinas.FindAsync(idClienteRutina);

            if (confirmacion == null || confirmacion.EstadoAsistencia != "ACEPTADO")
            {
                TempData["ErrorMessage"] = "No se encontró una confirmación válida.";
                return RedirectToAction("Index");
            }

            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);

            bool yaRegistrada = await _context.Asistencia.AnyAsync(a =>
                a.IdCliente == confirmacion.IdCliente && a.Fecha == hoy);

            if (yaRegistrada)
            {
                TempData["ErrorMessage"] = "La asistencia de hoy ya fue registrada.";
                return RedirectToAction("Index");
            }

            _context.Asistencia.Add(new Asistencium
            {
                IdCliente = confirmacion.IdCliente,
                Fecha = hoy,
                HoraEntrada = TimeOnly.FromDateTime(DateTime.Now)
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Asistencia registrada correctamente.";
            return RedirectToAction("Index");
        }

        // Pone al día a clientes creados después de publicarse el último WOD vigente
        private async Task PonerAlDiaConElUltimoWod(int idCliente, DateOnly hoy)
        {
            var ultimoWod = await _context.Rutinas
                .OrderByDescending(r => r.IdRutina)
                .FirstOrDefaultAsync();

            if (ultimoWod == null)
            {
                return;
            }

            bool yaTieneAsignacion = await _context.ClienteRutinas
                .AnyAsync(cr => cr.IdCliente == idCliente && cr.IdRutina == ultimoWod.IdRutina);

            if (yaTieneAsignacion)
            {
                return;
            }

            DateOnly? fechaVigente = await _context.ClienteRutinas
                .Where(cr => cr.IdRutina == ultimoWod.IdRutina)
                .Select(cr => (DateOnly?)cr.FechaAsignacion)
                .FirstOrDefaultAsync();

            // No se reactiva si ya quedó en el pasado
            if (fechaVigente == null || fechaVigente < hoy)
            {
                return;
            }

            _context.ClienteRutinas.Add(new ClienteRutina
            {
                IdCliente = idCliente,
                IdRutina = ultimoWod.IdRutina,
                FechaAsignacion = fechaVigente.Value,
                EstadoAsistencia = "PENDIENTE"
            });

            await _context.SaveChangesAsync();
        }
    }
}
