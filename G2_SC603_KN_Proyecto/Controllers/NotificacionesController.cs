using G2_SC603_KN_Proyecto.Models;
using Microsoft.AspNetCore.Mvc;

namespace G2_SC603_KN_Proyecto.Controllers
{
    public class NotificacionesController : Controller
    {
        private readonly DbOrionFitContext _context;

        public NotificacionesController(DbOrionFitContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            int? idUsuario = HttpContext.Session.GetInt32("ID");

            if (idUsuario == null)
                return RedirectToAction("Index", "Home");

            var cliente = _context.Clientes
                .FirstOrDefault(c => c.IdUsuario == idUsuario);

            if (cliente == null)
                return RedirectToAction("Home", "Home");

            var notificaciones = _context.Notificaciones
                .Where(n => n.IdCliente == cliente.IdCliente)
                .OrderByDescending(n => n.Fecha)
                .ToList();

            return View(notificaciones);
        }
    }
}