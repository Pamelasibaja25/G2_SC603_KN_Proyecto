using Microsoft.EntityFrameworkCore;
using G2_SC603_KN_Proyecto.Models;
using G2_SC603_KN_Proyecto.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace G2_SC603_KN_Proyecto.Controllers
{
    public class HomeController : Controller
    {
        private readonly DbOrionFitContext _context;

        private static readonly ConcurrentDictionary<string, (int intentos, DateTime bloqueadoHasta, DateTime ultimoFallo)> _intentosFallidos = new();
        private const int MaxIntentos = 5;
        private static readonly TimeSpan TiempoBloqueo = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan VentanaIntentos = TimeSpan.FromMinutes(15);

        public HomeController(DbOrionFitContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string username, string password)
        {
            string claveIntentos = (username ?? "").Trim().ToLower();

            if (_intentosFallidos.TryGetValue(claveIntentos, out var estadoActual))
            {
                if (estadoActual.bloqueadoHasta > DateTime.Now)
                {
                    int minutosRestantes = (int)Math.Ceiling((estadoActual.bloqueadoHasta - DateTime.Now).TotalMinutes);
                    ViewBag.Error = $"Demasiados intentos fallidos. Probá de nuevo en {minutosRestantes} minuto(s).";
                    return View();
                }

                bool bloqueoVencido = estadoActual.bloqueadoHasta != DateTime.MinValue;
                bool fallosViejos = DateTime.Now - estadoActual.ultimoFallo > VentanaIntentos;
                if (bloqueoVencido || fallosViejos)
                    _intentosFallidos.TryRemove(claveIntentos, out _);
            }

            var user = _context.Usuarios
                .Include(u => u.Clientes)
                .FirstOrDefault(u => u.Username == username);

            if (user != null && PasswordHasher.Verify(password, user.Contrasena, out bool esHashViejo))
            {
                if (esHashViejo)
                {
                    user.Contrasena = PasswordHasher.HashPassword(password);
                    _context.SaveChanges();
                }

                _intentosFallidos.TryRemove(claveIntentos, out _);

                if (!user.Activo)
                {
                    ViewBag.Error = "Esta cuenta está desactivada. Contactá al administrador.";
                    return View();
                }

                if (user.Rol.ToUpper() == "USER")
                {
                    var cliente = user.Clientes.FirstOrDefault();

                    if (cliente == null)
                    {
                        ViewBag.Error = "No existe un cliente asociado a este usuario.";
                        return View();
                    }

                    bool tieneAcceso = _context.ClienteMembresia.Any(cm =>
                        cm.IdCliente == cliente.IdCliente &&
                        (
                            (cm.Estado == "Activa" && cm.FechaFin >= ZonaHoraria.Hoy)
                            || cm.Estado == "Pendiente"
                        ));

                    if (!tieneAcceso)
                    {
                        ViewBag.Error = "La membresía no se encuentra activa.";
                        return View();
                    }
                }

                HttpContext.Session.SetString("Usuario", user.Username);
                HttpContext.Session.SetString("Rol", user.Rol.ToUpper());
                HttpContext.Session.SetInt32("ID", user.IdUsuario); // ← ya estaba, confirmado

                return RedirectToAction("Home", "Home");
            }

            _intentosFallidos.AddOrUpdate(claveIntentos,
                _ => (1, DateTime.MinValue, DateTime.Now),
                (_, actual) =>
                {
                    int nuevosIntentos = actual.intentos + 1;
                    DateTime bloqueo = nuevosIntentos >= MaxIntentos
                        ? DateTime.Now.Add(TiempoBloqueo)
                        : DateTime.MinValue;
                    return (nuevosIntentos, bloqueo, DateTime.Now);
                });

            ViewBag.Error = "Usuario o contraseña incorrectos";
            return View();
        }

        public IActionResult Home()
        {
            int? idUsuario = HttpContext.Session.GetInt32("ID");

            if (idUsuario == null)
                return RedirectToAction("Index", "Home");

            var cliente = _context.Clientes
                .FirstOrDefault(c => c.IdUsuario == idUsuario);

            if (cliente == null)
            {
                ViewBag.MembresiaActiva = null;
                return View();
            }

            var membresiaActiva = _context.ClienteMembresia
                .FirstOrDefault(cm =>
                    cm.IdCliente == cliente.IdCliente &&
                    cm.Estado.Trim().ToLower() == "activa" &&
                    cm.FechaFin >= ZonaHoraria.Hoy
                );

            ViewBag.MembresiaActiva = membresiaActiva;

            if (membresiaActiva == null)
            {
                ViewBag.MembresiaPendiente = _context.ClienteMembresia
                    .FirstOrDefault(cm =>
                        cm.IdCliente == cliente.IdCliente &&
                        cm.Estado.Trim().ToLower() == "pendiente");
            }

            if (membresiaActiva != null)
                GenerarNotificacionVencimientoSiCorresponde(cliente.IdCliente, membresiaActiva.FechaFin);

            return View();
        }

        private void GenerarNotificacionVencimientoSiCorresponde(int idCliente, DateOnly fechaFin)
        {
            DateOnly hoy = ZonaHoraria.Hoy;
            int diasRestantes = fechaFin.DayNumber - hoy.DayNumber;

            if (diasRestantes < 0 || diasRestantes > 5)
                return;

            bool yaNotificadoHoy = _context.Notificaciones.Any(n =>
                n.IdCliente == idCliente &&
                n.Tipo == "Vencimiento" &&
                n.Fecha.Date == ZonaHoraria.HoyDateTime);

            if (yaNotificadoHoy)
                return;

            _context.Notificaciones.Add(new Notificacion
            {
                IdCliente = idCliente,
                Tipo = "Vencimiento",
                Titulo = "Tu mensualidad está por vencer",
                Mensaje = diasRestantes == 0
                    ? "Tu mensualidad vence hoy. Renová para seguir entrenando sin interrupciones."
                    : $"Tu mensualidad vence en {diasRestantes} día(s). Renová a tiempo.",
                Fecha = DateTime.Now,
                Leida = false
            });

            _context.SaveChanges();
        }

        // ── NUEVO: Perfil y cambio de contraseña (AUTH-003) ───────────────────────

        [HttpGet]
        public IActionResult Perfil()
        {
            int? idUsuario = HttpContext.Session.GetInt32("ID");
            if (idUsuario == null)
                return RedirectToAction("Index");

            var user = _context.Usuarios.Find(idUsuario.Value);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CambiarContrasena(string contrasenaActual, string nuevaContrasena, string confirmarContrasena)
        {
            int? idUsuario = HttpContext.Session.GetInt32("ID");
            if (idUsuario == null)
                return RedirectToAction("Index");

            if (string.IsNullOrWhiteSpace(nuevaContrasena) || nuevaContrasena != confirmarContrasena)
            {
                TempData["ErrorMessage"] = "Las contraseñas no coinciden o están vacías.";
                return RedirectToAction("Perfil");
            }

            var user = _context.Usuarios.Find(idUsuario.Value);

            if (user == null || !PasswordHasher.Verify(contrasenaActual, user.Contrasena, out _))
            {
                TempData["ErrorMessage"] = "La contraseña actual es incorrecta.";
                return RedirectToAction("Perfil");
            }

            try
            {
                string nuevoHash = PasswordHasher.HashPassword(nuevaContrasena);
                user.Contrasena = nuevoHash;
                _context.SaveChanges();
                TempData["MensajeExito"] = "Contraseña actualizada correctamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al actualizar la contraseña: " + ex.Message;
            }

            return RedirectToAction("Perfil");
        }

        #region Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
        #endregion
    }
}