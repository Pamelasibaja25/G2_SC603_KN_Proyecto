using Microsoft.AspNetCore.Mvc;

public class EquiposController : Controller
{
    public IActionResult controlDeEquipos()
    {
        ViewData["Mensaje"] = "Control de Equipos y Productos"; 

        return View("controlDeEquipos");
    }
}