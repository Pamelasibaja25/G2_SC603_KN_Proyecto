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

        public async Task<IActionResult> Index(string? filtroEstado = null, string? mes = null)
        {
            // "mes" viene como "yyyy-MM" (ej: "2026-08"). Si no se manda,
            // se usa el mes actual por defecto para no cargar todo el historial de una.
            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);
            DateOnly mesSeleccionado = hoy;
            if (!string.IsNullOrEmpty(mes) && DateOnly.TryParse(mes + "-01", out DateOnly parsed))
            {
                mesSeleccionado = parsed;
            }
            DateOnly inicioMes = new DateOnly(mesSeleccionado.Year, mesSeleccionado.Month, 1);
            DateOnly finMes = inicioMes.AddMonths(1).AddDays(-1);

            string rol = HttpContext.Session.GetString("Rol") ?? string.Empty;
            int? idUsuario = HttpContext.Session.GetInt32("ID");

            Cliente? cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.IdUsuario == idUsuario);

            ReservasViewModel model = new ReservasViewModel();
            model.EsAdmin = rol.Contains("ADMIN") || rol.Contains("RECEPTION");
            model.EsCliente = cliente != null;

            if (cliente != null)
            {
                await PonerAlDiaConElUltimoWod(cliente.IdCliente, hoy);

                // Todas las confirmaciones del cliente, sin filtrar (para calcular el WOD
                // pendiente actual, que siempre debe verse sin importar qué mes se esté mirando).
                List<ConfirmacionWodVM> todasSusConfirmaciones = await _context.ClienteRutinas
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
                        Estado = cr.EstadoAsistencia,
                        Horarios = cr.Horarios
                    })
                    .ToListAsync();

                model.WodPendienteActual = todasSusConfirmaciones
                    .FirstOrDefault(c => c.Estado == "PENDIENTE" && c.Fecha >= hoy);

                // Historial (tabla de abajo): sí se filtra por el mes elegido.
                model.MisConfirmaciones = todasSusConfirmaciones
                    .Where(c => c.Fecha >= inicioMes && c.Fecha <= finMes)
                    .ToList();

                // Votos de la encuesta de horarios para el WOD más reciente del cliente,
                // para mostrar el conteo estilo encuesta de WhatsApp mientras elige.
                var wodPendiente = model.WodPendienteActual;
                if (wodPendiente != null)
                {
                    List<string?> horariosDeTodos = await _context.ClienteRutinas
                        .Where(cr => cr.IdRutina == wodPendiente.IdRutina
                            && cr.EstadoAsistencia == "ACEPTADO"
                            && cr.Horarios != null)
                        .Select(cr => cr.Horarios)
                        .ToListAsync();

                    foreach (string h in HorariosWod.Opciones)
                    {
                        model.VotosPorHorario[h] = horariosDeTodos
                            .Count(hs => hs != null && hs.Split(',').Contains(h));
                    }

                    model.TotalVotantesHorario = horariosDeTodos.Count;
                }
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
                var idsClienteRutinaYaAsistidos = await _context.Asistencia
                    .Where(a => a.Fecha == hoy && a.IdClienteRutina != null)
                    .Select(a => a.IdClienteRutina!.Value)
                    .ToListAsync();

                model.ConfirmadosHoy = confirmadosHoy.Select(cr => new ConfirmacionWodVM
                {
                    IdClienteRutina = cr.IdClienteRutina,
                    IdRutina = cr.IdRutina,
                    NombreWod = cr.IdRutinaNavigation.Nombre,
                    NombreCliente = cr.IdClienteNavigation.Nombre,
                    Fecha = cr.FechaAsignacion,
                    Estado = cr.EstadoAsistencia,
                    Horarios = cr.Horarios,
                    AsistioHoy = idsClienteRutinaYaAsistidos.Contains(cr.IdClienteRutina)
                }).ToList();

                IQueryable<ClienteRutina> query = _context.ClienteRutinas
                    .Include(cr => cr.IdRutinaNavigation)
                    .Include(cr => cr.IdClienteNavigation)
                    .Where(cr => cr.FechaAsignacion >= inicioMes && cr.FechaAsignacion <= finMes);

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
                                ConfirmoPorWod = wods.Select(w => cg.Any(x => x.IdRutinaNavigation.Nombre == w)).ToList(),
                                Horarios = cg.Select(x => x.Horarios).FirstOrDefault(h => !string.IsNullOrEmpty(h))
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

            model.MesSeleccionado = mesSeleccionado.ToString("yyyy-MM");

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
                a.IdClienteRutina == idClienteRutina);

            if (yaRegistrada)
            {
                TempData["ErrorMessage"] = "El check-in de este WOD ya fue registrado.";
                return RedirectToAction("Index");
            }

            _context.Asistencia.Add(new Asistencium
            {
                IdCliente = confirmacion.IdCliente,
                IdClienteRutina = idClienteRutina,
                Fecha = hoy,
                HoraEntrada = TimeOnly.FromDateTime(DateTime.Now)
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Asistencia registrada correctamente.";
            return RedirectToAction("Index");
        }

        // Pone al día a clientes creados después de publicarse un WOD vigente
        // (puede haber más de uno publicado para el mismo día).
        private async Task PonerAlDiaConElUltimoWod(int idCliente, DateOnly hoy)
        {
            // Todas las rutinas que ALGÚN cliente ya tiene asignadas para hoy
            // o más adelante: son las que siguen "vigentes".
            var rutinasVigentes = await _context.ClienteRutinas
                .Where(cr => cr.FechaAsignacion >= hoy)
                .Select(cr => new { cr.IdRutina, cr.FechaAsignacion })
                .Distinct()
                .ToListAsync();

            if (!rutinasVigentes.Any())
            {
                return;
            }

            var idsYaAsignados = await _context.ClienteRutinas
                .Where(cr => cr.IdCliente == idCliente)
                .Select(cr => cr.IdRutina)
                .ToListAsync();

            bool huboCambios = false;

            foreach (var rutina in rutinasVigentes)
            {
                if (idsYaAsignados.Contains(rutina.IdRutina))
                {
                    continue;
                }

                _context.ClienteRutinas.Add(new ClienteRutina
                {
                    IdCliente = idCliente,
                    IdRutina = rutina.IdRutina,
                    FechaAsignacion = rutina.FechaAsignacion,
                    EstadoAsistencia = "PENDIENTE"
                });
                huboCambios = true;
            }

            if (huboCambios)
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
