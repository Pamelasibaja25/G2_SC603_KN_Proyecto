using G2_SC603_KN_Proyecto.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace G2_SC603_KN_Proyecto.Controllers
{
    public class ClientesController : Controller
    {

        public IActionResult RegistrarClientes()
        {
            ViewData["Mensaje"] = "Registro de Clientes";
            return View();
        }
        public IActionResult MostrarClientes()
        {
            ViewData["Mensaje"] = "Lista de Clientes";
            return View();
        }
    }
}
