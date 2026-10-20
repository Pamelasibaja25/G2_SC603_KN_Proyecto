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
            if (idUsuario == null) return RedirectToAction("Index", "Home");

            var cliente = _context.Clientes.FirstOrDefault(c => c.IdUsuario == (int)idUsuario);
            if (cliente == null) return RedirectToAction("Index", "Home");

            var notificaciones = _context.Notificaciones
                .Where(n => n.IdCliente == cliente.IdCliente)
                .OrderByDescending(n => n.Fecha)
                .ToList();

            return View(notificaciones);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult MarcarLeidas()
        {
            int? idUsuario = HttpContext.Session.GetInt32("ID");
            if (idUsuario == null) return Json(new { ok = false });

            var cliente = _context.Clientes.FirstOrDefault(c => c.IdUsuario == (int)idUsuario);
            if (cliente == null) return Json(new { ok = false });

            var sinLeer = _context.Notificaciones
                .Where(n => n.IdCliente == cliente.IdCliente && !n.Leida).ToList();
            sinLeer.ForEach(n => n.Leida = true);
            if (sinLeer.Any()) _context.SaveChanges();

            return Json(new { ok = true });
        }
    }
}