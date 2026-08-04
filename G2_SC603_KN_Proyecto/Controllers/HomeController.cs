
using Microsoft.EntityFrameworkCore;
using G2_SC603_KN_Proyecto.Models;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto.Generators;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace G2_SC603_KN_Proyecto.Controllers
{
    public class HomeController : Controller
    {
        private readonly DbOrionFitContext _context;

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
            var user = _context.Usuarios
                .Include(u => u.Clientes)
                .FirstOrDefault(u => u.Username == username);

            Console.WriteLine($"Usuario encontrado: {user?.Username}");
            Console.WriteLine($"Hash en DB: {user?.Contrasena}");

            if (user != null)
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                    string hashIngresado = BitConverter.ToString(bytes)
                        .Replace("-", "")
                        .ToLower();

                    Console.WriteLine($"Hash ingresado: {hashIngresado}");
                    Console.WriteLine($"Son iguales: {hashIngresado == user.Contrasena.ToLower()}");

                    if (hashIngresado == user.Contrasena.ToLower())
                    {
                        // Validar membresía activa únicamente para usuarios USER
                        if (user.Rol.ToUpper() == "USER")
                        {
                            var cliente = user.Clientes.FirstOrDefault();

                            if (cliente == null)
                            {
                                ViewBag.Error = "No existe un cliente asociado a este usuario.";
                                return View();
                            }

                            bool membresiaActiva = _context.ClienteMembresia.Any(cm =>
                                cm.IdCliente == cliente.IdCliente &&
                                cm.Estado == "Activa" &&
                                cm.FechaFin >= DateOnly.FromDateTime(DateTime.Today));

                            if (!membresiaActiva)
                            {
                                ViewBag.Error = "La membresía no se encuentra activa.";
                                return View();
                            }
                        }

                        HttpContext.Session.SetString("Usuario", user.Username);
                        HttpContext.Session.SetString("Rol", user.Rol);
                        HttpContext.Session.SetInt32("ID", user.IdUsuario);

                        return RedirectToAction("Home", "Home");
                    }
                }
            }

            ViewBag.Error = "Usuario o contraseña incorrectos";
            return View();
        }

        /**
        [HttpPost]
        public IActionResult Index(string username, string password)
        {
            // BYPASS TEMPORAL PARA DESARROLLO
            if (username == "devadmin" && password == "dev123")
            {
                HttpContext.Session.SetString("Usuario", "devadmin");
                HttpContext.Session.SetString("Rol", "ADMIN"); 
                return RedirectToAction("Home", "Home");
            }

            var user = _context.Usuarios.FirstOrDefault(u => u.Username == username);
            if (user != null)
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                    string hashIngresado = BitConverter.ToString(bytes).Replace("-", "").ToLower();

                    if (hashIngresado == user.Contrasena.ToLower())
                    {
                        HttpContext.Session.SetString("Usuario", user.Username);
                        HttpContext.Session.SetString("Rol", user.Rol);
                        return RedirectToAction("Home", "Home");
                    }
                }
            }

            ViewBag.Error = "Usuario o contraseña incorrectos";
            return View();
        }
        **/

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
                    cm.FechaFin >= DateOnly.FromDateTime(DateTime.Today)
                );

            ViewBag.MembresiaActiva = membresiaActiva;

            if (membresiaActiva != null)
            {
                GenerarNotificacionVencimientoSiCorresponde(cliente.IdCliente, membresiaActiva.FechaFin);
            }

            return View();
        }

        // Notifica al cliente cuando su mensualidad vence en 5 días o menos.
        // Evita duplicar la notificación si ya se generó hoy mismo.
        private void GenerarNotificacionVencimientoSiCorresponde(int idCliente, DateOnly fechaFin)
        {
            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);
            int diasRestantes = fechaFin.DayNumber - hoy.DayNumber;

            if (diasRestantes < 0 || diasRestantes > 5)
            {
                return;
            }

            bool yaNotificadoHoy = _context.Notificaciones.Any(n =>
                n.IdCliente == idCliente &&
                n.Tipo == "Vencimiento" &&
                n.Fecha.Date == DateTime.Today);

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
