using G2_SC603_KN_Proyecto.Models;
using G2_SC603_KN_Proyecto.Filters;
using G2_SC603_KN_Proyecto.Models.ViewModels.Wod;
using G2_SC603_KN_Proyecto.Services.Wod;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G2_SC603_KN_Proyecto.Controllers
{
    public class WODController : Controller
    {
        private readonly DbOrionFitContext _context;
        private readonly IWodConsultaService _wodConsultaService;
        private readonly IWodEliminacionService _wodEliminacionService;
        private readonly IWebHostEnvironment _env;

        public WODController(
            DbOrionFitContext context,
            IWodConsultaService wodConsultaService,
            IWodEliminacionService wodEliminacionService,
            IWebHostEnvironment env)
        {
            _context = context;
            _wodConsultaService = wodConsultaService;
            _wodEliminacionService = wodEliminacionService;
            _env = env;
        }

        #region Mostrar WODs
        public async Task<IActionResult> MostrarWOD()
        {
            List<EjercicioResumen> ejercicios = await _context.EjerciciosResumen
                .FromSqlRaw("CALL sp_obtenerEjercicios()")
                .ToListAsync();
            List<WodResumen> wods = await _context.WodsResumen
                .FromSqlRaw("CALL sp_obtenerWODs()")
                .ToListAsync();

            ViewBag.Wods = wods;

            // Fecha asignada al cliente para cada WOD (se muestra junto a la imagen)
            string usernameActual = HttpContext.Session.GetString("Usuario") ?? string.Empty;
            Cliente? clienteActual = await _context.Clientes
                .Include(c => c.IdUsuarioNavigation)
                .FirstOrDefaultAsync(c => c.IdUsuarioNavigation.Username == usernameActual);

            if (clienteActual != null)
            {
                await PonerAlDiaConElUltimoWod(clienteActual.IdCliente, DateOnly.FromDateTime(DateTime.Today));

                ViewBag.FechasPorRutina = await _context.ClienteRutinas
                    .Where(cr => cr.IdCliente == clienteActual.IdCliente)
                    .GroupBy(cr => cr.IdRutina)
                    .Select(g => new { IdRutina = g.Key, Fecha = g.Max(cr => cr.FechaAsignacion) })
                    .ToDictionaryAsync(x => x.IdRutina, x => x.Fecha);
            }

            return View(ejercicios);
        }
        #endregion

        #region Agregar WOD
        [HttpPost]
        public async Task<IActionResult> AgregarWOD(string nombre, string objetivo, IFormFile? imagen)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    TempData["ErrorMessage"] = "El nombre del entrenamiento es obligatorio.";
                    return RedirectToAction("MostrarWOD");
                }

                if (imagen == null || imagen.Length == 0)
                {
                    TempData["ErrorMessage"] = "Debe adjuntar la imagen del entrenamiento (es la única información que ve el cliente).";
                    return RedirectToAction("MostrarWOD");
                }

                string usernameActual = HttpContext.Session.GetString("Usuario") ?? string.Empty;

                Entrenador? entrenador = await _context.Entrenadors
                    .Include(e => e.IdUsuarioNavigation)
                    .FirstOrDefaultAsync(e => e.IdUsuarioNavigation.Username == usernameActual);

                if (entrenador == null)
                {
                    // Fallback si el usuario logueado no es entrenador (ej: ADMIN publicando)
                    entrenador = await _context.Entrenadors.FirstOrDefaultAsync()
                        ?? throw new Exception("No hay entrenadores registrados en el sistema.");
                }

                string? rutaImagen = await GuardarImagenWod(imagen);

                await _context.Database.ExecuteSqlRawAsync(
                    "CALL sp_agregarWOD({0}, {1}, {2}, {3}, {4})",
                    entrenador.IdEntrenador,
                    nombre,
                    objetivo ?? string.Empty,
                    rutaImagen,
                    "[]"
                );

                // El WOD recién creado es el de mayor Id (sp_AgregarWOD no retorna el Id).
                int idRutinaNueva = await _context.Rutinas
                    .OrderByDescending(r => r.IdRutina)
                    .Select(r => r.IdRutina)
                    .FirstAsync();

                AsignarWodClientesParaManana(idRutinaNueva);

                TempData["SuccessMessage"] = "WOD publicado correctamente para mañana.";
                GenerarNotificacionWOD(nombre, objetivo);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al publicar el WOD: " + ex.Message;
            }

            return RedirectToAction("MostrarWOD");
        }
        #endregion
        // El WOD publicado hoy se asigna como el de mañana a todos los clientes activos (estado PENDIENTE)
        private void AsignarWodClientesParaManana(int idRutina)
        {
            DateOnly manana = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
            var clientes = _context.Clientes.ToList();

            foreach (var cliente in clientes)
            {
                _context.ClienteRutinas.Add(new ClienteRutina
                {
                    IdCliente = cliente.IdCliente,
                    IdRutina = idRutina,
                    FechaAsignacion = manana,
                    EstadoAsistencia = "PENDIENTE"
                });
            }

            _context.SaveChanges();
        }

        // Cliente confirma o rechaza el WOD de mañana
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarAsistenciaWOD(int idClienteRutina, string estado, List<string>? horarios)
        {
            if (estado != "ACEPTADO" && estado != "NO_ASISTE")
            {
                TempData["ErrorMessage"] = "Estado de asistencia inválido.";
                return RedirectToAction(nameof(EntrenamientoDiario));
            }

            (int idUsuario, string rol) = ObtenerUsuarioActual();
            int? idCliente = await _context.Clientes
                .Where(c => c.IdUsuario == idUsuario)
                .Select(c => (int?)c.IdCliente)
                .FirstOrDefaultAsync();

            var registro = await _context.ClienteRutinas
                .FirstOrDefaultAsync(cr => cr.IdClienteRutina == idClienteRutina
                    && cr.IdCliente == idCliente);

            if (registro == null)
            {
                TempData["ErrorMessage"] = "No se encontró el WOD asignado.";
                return RedirectToAction(nameof(EntrenamientoDiario));
            }

            registro.EstadoAsistencia = estado;

            // Solo guarda horarios si acepta; solo se permiten los del catálogo fijo.
            registro.Horarios = estado == "ACEPTADO" && horarios != null && horarios.Any()
                ? string.Join(",", horarios.Where(h => HorariosWod.Opciones.Contains(h)))
                : null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = estado == "ACEPTADO"
                ? "¡Confirmaste tu asistencia al WOD!"
                : "Quedó registrado que no asistirás al WOD.";

            string referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer);
            }

            return RedirectToAction(nameof(EntrenamientoDiario));
        }

        private void GenerarNotificacionWOD(string nombre, string objetivo)
        {
            var clientes = _context.Clientes.ToList();

            foreach (var cliente in clientes)
            {
                _context.Notificaciones.Add(new Notificacion
                {
                    IdCliente = cliente.IdCliente,
                    Tipo = "WOD",
                    Titulo = "Nuevo entrenamiento disponible",
                    Mensaje = $"Se publicó el WOD: {nombre}. Objetivo: {objetivo}",
                    Fecha = DateTime.Now,
                    Leida = false
                });
            }

            _context.SaveChanges();
        }
        private async Task<string?> GuardarImagenWod(IFormFile? imagen)
        {
            if (imagen == null || imagen.Length == 0)
            {
                return null;
            }

            string extension = Path.GetExtension(imagen.FileName).ToLower();
            string[] permitidas = { ".jpg", ".jpeg", ".png", ".webp" };

            if (!permitidas.Contains(extension))
            {
                throw new Exception("La imagen debe ser jpg, png o webp.");
            }

            string carpeta = Path.Combine(_env.WebRootPath, "img", "wods");
            Directory.CreateDirectory(carpeta);

            string nombreArchivo = Guid.NewGuid().ToString("N") + extension;
            string rutaFisica = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                await imagen.CopyToAsync(stream);
            }

            return "img/wods/" + nombreArchivo;
        }

        #region Editar WOD

        [HttpGet]
        public async Task<IActionResult> EditarWOD(int id)
        {
            var rol = HttpContext.Session.GetString("Rol") ?? string.Empty;
            if (!rol.Contains("ADMIN") && !rol.Contains("TRAINER"))
                return RedirectToAction("MostrarWOD");

            var rutina = await _context.Rutinas.FindAsync(id);
            if (rutina == null)
            {
                TempData["ErrorMessage"] = "El WOD no existe.";
                return RedirectToAction("MostrarWOD");
            }

            return View(rutina);
        }

        [HttpPost]
        public async Task<IActionResult> EditarWOD(int idRutina, string nombre,
            string objetivo, IFormFile? imagen)
        {
            var rol = HttpContext.Session.GetString("Rol") ?? string.Empty;
            if (!rol.Contains("ADMIN") && !rol.Contains("TRAINER"))
                return RedirectToAction("MostrarWOD");

            if (string.IsNullOrWhiteSpace(nombre))
            {
                TempData["ErrorMessage"] = "El nombre del entrenamiento es obligatorio.";
                return RedirectToAction("EditarWOD", new { id = idRutina });
            }

            try
            {
                var rutina = await _context.Rutinas.FindAsync(idRutina);
                if (rutina == null)
                {
                    TempData["ErrorMessage"] = "El WOD no existe.";
                    return RedirectToAction("MostrarWOD");
                }

                rutina.Nombre = nombre;
                rutina.Objetivo = objetivo ?? string.Empty;

                if (imagen != null && imagen.Length > 0)
                {
                    string? rutaImagen = await GuardarImagenWod(imagen);
                    if (!string.IsNullOrEmpty(rutaImagen))
                    {
                        rutina.Imagen = rutaImagen;
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "WOD actualizado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al actualizar: " + ex.Message;
                return RedirectToAction("EditarWOD", new { id = idRutina });
            }

            return RedirectToAction("MostrarWOD");
        }
        #region Eliminar WOD 
        // La confirmación ocurre en el cliente (ver MostrarWOD.cshtml). Requiere POST + AntiForgeryToken + rol ADMIN.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RolAutorizado("ADMIN")]
        public async Task<IActionResult> EliminarWOD(int id)
        {
            (bool exito, string mensaje) = await _wodEliminacionService.EliminarRutinaAsync(id);

            TempData[exito ? "SuccessMessage" : "ErrorMessage"] = mensaje;

            return RedirectToAction(nameof(MostrarWOD));
        }
        #endregion
        #endregion

        #region Entrenamiento Diario (RMGM-WOD-002)
        [HttpGet]
        public async Task<IActionResult> EntrenamientoDiario()
        {
            (int idUsuario, string rol) = ObtenerUsuarioActual();

            if (rol.Contains("USER"))
            {
                Cliente? clienteActual = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.IdUsuario == idUsuario);

                if (clienteActual != null)
                {
                    await PonerAlDiaConElUltimoWod(clienteActual.IdCliente, DateOnly.FromDateTime(DateTime.Today));
                }
            }

            List<WodHistorialItemViewModel> entrenamientoDiario =
                await _wodConsultaService.ObtenerEntrenamientoDiarioAsync(idUsuario, rol);

            return View(entrenamientoDiario);
        }
        #endregion

        #region Detalle de Entrenamiento
        // Vista de detalle compartida entre admin y cliente
        [HttpGet]
        public async Task<IActionResult> DetalleEntrenamiento(int id)
        {
            (int idUsuario, string rol) = ObtenerUsuarioActual();

            WodDetalleViewModel? detalle =
                await _wodConsultaService.ObtenerDetalleAsync(id, idUsuario, rol);

            if (detalle == null)
            {
                TempData["ErrorMessage"] = "El entrenamiento no existe o no tiene acceso a este registro.";
                return RedirectToAction(nameof(EntrenamientoDiario));
            }

            return View(detalle);
        }
        #endregion

        /// Obtiene el id de usuario y el rol desde la sesión actual.
        /// Centraliza esta lectura para evitar duplicar el acceso a
        /// HttpContext.Session en cada acción (DRY).
      
        private (int IdUsuario, string Rol) ObtenerUsuarioActual()
        {
            int idUsuario = HttpContext.Session.GetInt32("ID") ?? 0;
            string rol = HttpContext.Session.GetString("Rol") ?? string.Empty;
            return (idUsuario, rol);
        }

        // Pone al día a clientes creados después de publicarse el último WOD vigente
        private async Task PonerAlDiaConElUltimoWod(int idCliente, DateOnly hoy)
        {
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