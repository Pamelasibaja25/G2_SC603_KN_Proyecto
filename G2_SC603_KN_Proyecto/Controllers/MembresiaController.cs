using G2_SC603_KN_Proyecto.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace G2_SC603_KN_Proyecto.Controllers
{
    public class MembresiaController : Controller
    {

        public IActionResult RegistrarMembresia()
        {
            ViewData["Mensaje"] = "Registro de Membresías";
            return View();
        }
        public IActionResult MostrarMembresia()
        {
            ViewData["Mensaje"] = "Lista de Membresías";
            return View();
        }
    }
}
