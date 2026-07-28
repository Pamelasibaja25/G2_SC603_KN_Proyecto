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
                .Include(p => p.idClienteMembresiaNavigation)
                .ThenInclude(cm => cm.IdClienteNavigation);

            // Si es USER, filtrar solo sus pagos
            if (rol == "USER")
            {
                query = query.Where(p =>
                    p.idClienteMembresiaNavigation.IdClienteNavigation.IdUsuario == idUsuario
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
        public async Task<IActionResult> RegistrarPago(Pago pago, IFormFile Comprobante)
        {
            if (Comprobante != null)
            {
                var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(Comprobante.FileName);

                var carpeta = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/comprobantes"
                );

                if (!Directory.Exists(carpeta))
                {
                    Directory.CreateDirectory(carpeta);
                }

                var ruta = Path.Combine(carpeta, nombreArchivo);

                using (var stream = new FileStream(ruta, FileMode.Create))
                {
                    await Comprobante.CopyToAsync(stream);
                }

                pago.comprobante = "/comprobantes/" + nombreArchivo;
            }

            _context.Pagos.Add(pago);
            await _context.SaveChangesAsync();

            GenerarNotificacionPago(pago.idClienteMembresia, pago.monto);

            return RedirectToAction("Index");
        }
        private void GenerarNotificacionPago(int idClienteMembresia, decimal monto)
        {
            var clienteMembresia = _context.ClienteMembresia
                .FirstOrDefault(cm => cm.IdClienteMembresia == idClienteMembresia);

            if (clienteMembresia == null) return;

            var notificacion = new Notificacion
            {
                idCliente = clienteMembresia.IdCliente,
                tipo = "Pago",
                titulo = "Pago registrado",
                mensaje = $"Se registró un pago de ₡{monto}",
                fecha = DateTime.Now,
                leida = false
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
                .Include(p => p.idClienteMembresiaNavigation)
                .ThenInclude(cm => cm.IdClienteNavigation)
                .FirstOrDefault(p => p.idPago == idPago);

            if (pago == null)
                return NotFound();

            return View(pago);
        }
    }
}
