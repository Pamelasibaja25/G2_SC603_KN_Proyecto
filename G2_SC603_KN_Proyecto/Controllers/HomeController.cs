using G2_SC603_KN_Proyecto.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace G2_SC603_KN_Proyecto.Controllers
{
    public class HomeController : Controller
    {
        private readonly DbOrionFitContext _context;

        public HomeController(DbOrionFitContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            
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
    }
}
