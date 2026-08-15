using G2_SC603_KN_Proyecto.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace G2_SC603_KN_Proyecto.Controllers
{
    public class AccountController : Controller
    {
        private readonly DbOrionFitContext _context;

        public AccountController(DbOrionFitContext context)
        {
            _context = context;
        }

        
        //  SHA256 (MISMO FORMATO QUE HOME)
        
        private string HashSHA256(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

                return BitConverter.ToString(bytes)
                    .Replace("-", "")
                    .ToLower();
            }
        }

        
        //  RECUPERAR CONTRASEÑA
      
        public IActionResult Recuperar()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Recuperar(string username, string correo, string nuevaPassword)
        {
            var usuario = _context.Usuarios
                .Include(u => u.Administradors)
                .Include(u => u.Entrenadors)
                .Include(u => u.Clientes)
                .FirstOrDefault(u => u.Username == username);

           
            string errorGenerico = "El usuario y el correo no coinciden con ningún registro.";

            if (usuario == null)
            {
                ViewBag.Error = errorGenerico;
                return View();
            }

            string correoIngresado = (correo ?? "").Trim().ToLower();

            bool correoCoincide =
                usuario.Administradors.Any(a => (a.Correo ?? "").Trim().ToLower() == correoIngresado) ||
                usuario.Entrenadors.Any(e => (e.Correo ?? "").Trim().ToLower() == correoIngresado) ||
                usuario.Clientes.Any(c => (c.Correo ?? "").Trim().ToLower() == correoIngresado);

            if (string.IsNullOrWhiteSpace(correoIngresado) || !correoCoincide)
            {
                ViewBag.Error = errorGenerico;
                return View();
            }

            usuario.Contrasena = HashSHA256(nuevaPassword.Trim());
            _context.SaveChanges();

            ViewBag.Mensaje = "Contraseña actualizada correctamente";
            return View();
        }

        
        //  CAMBIAR CONTRASEÑA
      
        public IActionResult Cambiar()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Cambiar(string actual, string nueva)
        {
            int? id = HttpContext.Session.GetInt32("ID");

            if (id == null)
                return RedirectToAction("Index", "Home");

            var usuario = _context.Usuarios
                .FirstOrDefault(u => u.IdUsuario == id);

            if (usuario == null)
                return RedirectToAction("Index", "Home");

            string actualHash = HashSHA256(actual.Trim());

            if (usuario.Contrasena != actualHash)
            {
                ViewBag.Error = "Contraseña actual incorrecta";
                return View();
            }

            usuario.Contrasena = HashSHA256(nueva.Trim());
            _context.SaveChanges();

            ViewBag.Mensaje = "Contraseña actualizada correctamente";
            return View();
        }
    }
}