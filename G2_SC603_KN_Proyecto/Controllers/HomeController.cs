using G2_SC603_KN_Proyecto.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace G2_SC603_KN_Proyecto.Controllers
{
    public class HomeController : Controller
    {

        public IActionResult Index()
        {
            ViewData["Mensaje"] = "Bienvenido";
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
