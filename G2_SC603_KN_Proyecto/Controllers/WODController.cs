using G2_SC603_KN_Proyecto.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace G2_SC603_KN_Proyecto.Controllers
{
    public class WODController : Controller
    {
        public IActionResult MostrarWOD()
        {
            return View();
        }
    }
}
