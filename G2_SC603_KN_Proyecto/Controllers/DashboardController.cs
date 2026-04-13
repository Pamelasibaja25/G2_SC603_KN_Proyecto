using Microsoft.AspNetCore.Mvc;

public class DashboardController : Controller
{
    public IActionResult Dashboard()
    {
        ViewData["Mensaje"] = "Dashboard Administrativo";
        return View();
    }
}