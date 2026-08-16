using G2_SC603_KN_Proyecto.Models;
using G2_SC603_KN_Proyecto.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace G2_SC603_KN_Proyecto.Controllers
{
    public class AccountController : Controller
    {
        private readonly DbOrionFitContext _context;

        public AccountController(DbOrionFitContext context)
        {
            _context = context;
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

            //mensaje de error generico para no dar pistas sobre si el usuario existe o no
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

            usuario.Contrasena = PasswordHasher.HashPassword(nuevaPassword.Trim());
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

            if (!PasswordHasher.Verify(actual.Trim(), usuario.Contrasena, out _))
            {
                ViewBag.Error = "Contraseña actual incorrecta";
                return View();
            }

            usuario.Contrasena = PasswordHasher.HashPassword(nueva.Trim());
            _context.SaveChanges();

            ViewBag.Mensaje = "Contraseña actualizada correctamente";
            return View();
        }
    }
}