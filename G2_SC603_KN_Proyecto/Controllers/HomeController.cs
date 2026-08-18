
using Microsoft.EntityFrameworkCore;
using G2_SC603_KN_Proyecto.Models;
using G2_SC603_KN_Proyecto.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace G2_SC603_KN_Proyecto.Controllers
{
    public class HomeController : Controller
    {
        private readonly DbOrionFitContext _context;

        // Protección contra fuerza bruta: solo en memoria del proceso (se
        // reinicia si la app se reinicia; para producción con más de una
        // instancia haría falta guardarlo en algo compartido como Redis).
        private static readonly ConcurrentDictionary<string, (int intentos, DateTime bloqueadoHasta)> _intentosFallidos = new();
        private const int MaxIntentos = 5;
        private static readonly TimeSpan TiempoBloqueo = TimeSpan.FromMinutes(5);

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

            if (_intentosFallidos.TryGetValue(claveIntentos, out var estadoActual)
                && estadoActual.bloqueadoHasta > DateTime.Now)
            {
                int minutosRestantes = (int)Math.Ceiling((estadoActual.bloqueadoHasta - DateTime.Now).TotalMinutes);
                ViewBag.Error = $"Demasiados intentos fallidos. Probá de nuevo en {minutosRestantes} minuto(s).";
                return View();
            }

            var user = _context.Usuarios
                .Include(u => u.Clientes)
                .FirstOrDefault(u => u.Username == username);

            if (user != null && PasswordHasher.Verify(password, user.Contrasena, out bool esHashViejo))
            {
                // Migración transparente: si todavía tenía el hash SHA-256
                // viejo, se reemplaza acá mismo por el hash seguro (PBKDF2 +
                // salt), sin que el usuario tenga que resetear nada.
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

                // Validar membresía activa únicamente para usuarios USER
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
                HttpContext.Session.SetInt32("ID", user.IdUsuario);

                return RedirectToAction("Home", "Home");
            }

            // Login fallido: cuenta el intento para el bloqueo temporal.
            _intentosFallidos.AddOrUpdate(claveIntentos,
                (1, DateTime.MinValue),
                (_, actual) =>
                {
                    int nuevosIntentos = actual.intentos + 1;
                    DateTime bloqueo = nuevosIntentos >= MaxIntentos
                        ? DateTime.Now.Add(TiempoBloqueo)
                        : DateTime.MinValue;
                    return (nuevosIntentos, bloqueo);
                });

            ViewBag.Error = "Usuario o contraseña incorrectos";
            return View();
        }

        public IActionResult Home()
        {
            int? idUsuario = HttpContext.Session.GetInt32("ID");

            if (idUsuario == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var cliente = _context.Clientes
                .FirstOrDefault(c => c.IdUsuario == idUsuario);

            // Si no es cliente (ej: admin), no mostramos membresía
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
            {
                GenerarNotificacionVencimientoSiCorresponde(cliente.IdCliente, membresiaActiva.FechaFin);
            }

            return View();
        }

        // Notifica al cliente si la mensualidad vence en 5 días o menos (una vez por día)
        private void GenerarNotificacionVencimientoSiCorresponde(int idCliente, DateOnly fechaFin)
        {
            DateOnly hoy = ZonaHoraria.Hoy;
            int diasRestantes = fechaFin.DayNumber - hoy.DayNumber;

            if (diasRestantes < 0 || diasRestantes > 5)
            {
                return;
            }

            bool yaNotificadoHoy = _context.Notificaciones.Any(n =>
                n.IdCliente == idCliente &&
                n.Tipo == "Vencimiento" &&
                n.Fecha.Date == ZonaHoraria.HoyDateTime);

            if (yaNotificadoHoy)
            {
                return;
            }

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
        #region Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
        #endregion
    }
}
