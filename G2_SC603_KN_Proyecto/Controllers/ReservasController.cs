using G2_SC603_KN_Proyecto.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G2_SC603_KN_Proyecto.Controllers
{
    // la reserva ES la confirmación de asistencia al WOD (Cliente_Rutina).
    // Flujo: admin publica el WOD de mañana -> cliente acepta/rechaza en
    // WOD/EntrenamientoDiario -> esa confirmación se refleja acá.
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
            model.EsAdmin = rol == "ADMIN" || rol == "RECEPTION";
            model.EsCliente = cliente != null;

            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);

            if (cliente != null)
            {
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
                // Confirmados para HOY: el WOD que se publicó ayer, ya es el de hoy.
                var confirmadosHoy = await _context.ClienteRutinas
                    .Include(cr => cr.IdRutinaNavigation)
                    .Include(cr => cr.IdClienteNavigation)
                    .Where(cr => cr.FechaAsignacion == hoy && cr.EstadoAsistencia == "ACEPTADO")
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

                // Historial completo, filtrable por estado.
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
            }

            return View(model);
        }

        // Admin: marca el check-in físico de un cliente que había confirmado asistencia.
        [HttpPost]
        public async Task<IActionResult> RegistrarAsistencia(int idClienteRutina)
        {
            string rol = HttpContext.Session.GetString("Rol") ?? string.Empty;
            if (rol != "ADMIN" && rol != "RECEPTION")
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
    }
}
