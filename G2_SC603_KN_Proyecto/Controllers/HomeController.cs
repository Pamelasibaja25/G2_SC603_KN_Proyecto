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


        /** [HttpPost]
         public IActionResult Index(string username, string password)
         {
             var user = _context.Usuarios.FirstOrDefault(u => u.Username == username);

             Console.WriteLine($"Usuario encontrado: {user?.Username}");
             Console.WriteLine($"Hash en DB: {user?.Contrasena}");

             if (user != null)
             {
                 using (SHA256 sha256 = SHA256.Create())
                 {
                     byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                     string hashIngresado = BitConverter.ToString(bytes).Replace("-", "").ToLower();

                     Console.WriteLine($"Hash ingresado: {hashIngresado}");
                     Console.WriteLine($"Son iguales: {hashIngresado == user.Contrasena.ToLower()}");

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
         }**/

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

        public IActionResult Home()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
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
