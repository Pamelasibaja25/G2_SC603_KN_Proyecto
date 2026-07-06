using G2_SC603_KN_Proyecto.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G2_SC603_KN_Proyecto.Controllers
{
    public class PagosController : Controller
    {
        private readonly DbOrionFitContext _context;

        public PagosController(DbOrionFitContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            string rol = HttpContext.Session.GetString("Rol") ?? "";
            int? idUsuario = HttpContext.Session.GetInt32("ID");

            IQueryable<Pago> query = _context.Pagos
                .Include(p => p.IdClienteMembresiaNavigation)
                .ThenInclude(cm => cm.IdClienteNavigation);

            // Si es USER, filtrar solo sus pagos
            if (rol == "USER")
            {
                query = query.Where(p =>
                    p.IdClienteMembresiaNavigation.IdClienteNavigation.IdUsuario == idUsuario
                );
            }

            List<Pago> pagos = query.ToList();

            ViewBag.Membresias = _context.ClienteMembresia
                .Include(cm => cm.IdClienteNavigation)
                .ToList();

            ViewBag.ClientesVencidos = _context.ClienteMembresia
                .Include(cm => cm.IdClienteNavigation)
                .Where(cm => cm.FechaFin < DateOnly.FromDateTime(DateTime.Today))
                .ToList();

            return View(pagos);
        }

        [HttpPost]
        public IActionResult RegistrarPago(Pago pago)
        {
            _context.Pagos.Add(pago);
            _context.SaveChanges();

            GenerarNotificacionPago(pago.IdClienteMembresia, pago.Monto);

            return RedirectToAction("Index");
        }
        public IActionResult HistorialCliente(int idCliente)
        {
            List<Pago> pagos = _context.Pagos
                .Include(p => p.IdClienteMembresiaNavigation)
                .Where(p => p.IdClienteMembresiaNavigation.IdCliente == idCliente)
                .ToList();

            return View(pagos);
        }
        private void GenerarNotificacionPago(int idClienteMembresia, decimal monto)
        {
            var clienteMembresia = _context.ClienteMembresia
                .FirstOrDefault(cm => cm.IdClienteMembresia == idClienteMembresia);

            if (clienteMembresia == null) return;

            var notificacion = new Notificacion
            {
                IdCliente = clienteMembresia.IdCliente,
                Tipo = "Pago",
                Titulo = "Pago registrado",
                Mensaje = $"Se registró un pago de ₡{monto}",
                Fecha = DateTime.Now,
                Leida = false
            };

            _context.Notificaciones.Add(notificacion);
            _context.SaveChanges();
        }
        [HttpPost]
        public IActionResult GenerarNotificacionPagoManual(int idClienteMembresia, decimal monto)
        {
            GenerarNotificacionPago(idClienteMembresia, monto);

            return Ok();
        }
        public IActionResult Comprobante(int idPago)
        {
            var pago = _context.Pagos
                .Include(p => p.IdClienteMembresiaNavigation)
                .ThenInclude(cm => cm.IdClienteNavigation)
                .FirstOrDefault(p => p.IdPago == idPago);

            if (pago == null)
                return NotFound();

            return View(pago);
        }
    }
}
