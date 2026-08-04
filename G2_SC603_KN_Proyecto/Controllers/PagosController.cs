using G2_SC603_KN_Proyecto.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G2_SC603_KN_Proyecto.Controllers
{
    public class PagosController : Controller
    {
        private readonly DbOrionFitContext _context;
        private readonly IWebHostEnvironment _env;

        public PagosController(DbOrionFitContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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
                .Include(cm => cm.IdMembresiaNavigation)
                .ToList();

            ViewBag.ClientesVencidos = _context.ClienteMembresia
                .Include(cm => cm.IdClienteNavigation)
                .Where(cm => cm.FechaFin < DateOnly.FromDateTime(DateTime.Today))
                .ToList();

            ViewBag.Sinpe = _context.ConfiguracionSinpe.FirstOrDefault();

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

        #region SINPE
        // Admin: sube o reemplaza la imagen con el QR / datos de cuenta SINPE.
        [HttpPost]
        public async Task<IActionResult> ConfigurarSinpe(IFormFile imagen)
        {
            string rol = HttpContext.Session.GetString("Rol") ?? "";
            if (rol != "ADMIN" && rol != "RECEPTION")
            {
                TempData["ErrorMessage"] = "No tiene permisos para esta acción.";
                return RedirectToAction("Index");
            }

            if (imagen == null || imagen.Length == 0)
            {
                TempData["ErrorMessage"] = "Debe seleccionar una imagen.";
                return RedirectToAction("Index");
            }

            string carpeta = Path.Combine(_env.WebRootPath, "img", "sinpe");
            Directory.CreateDirectory(carpeta);

            string nombreArchivo = Guid.NewGuid().ToString("N") + Path.GetExtension(imagen.FileName);
            string rutaFisica = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                await imagen.CopyToAsync(stream);
            }

            var config = await _context.ConfiguracionSinpe.FirstOrDefaultAsync();
            string rutaRelativa = "img/sinpe/" + nombreArchivo;

            if (config == null)
            {
                _context.ConfiguracionSinpe.Add(new ConfiguracionSinpe
                {
                    ImagenQr = rutaRelativa,
                    ActualizadoEn = DateTime.Now
                });
            }
            else
            {
                config.ImagenQr = rutaRelativa;
                config.ActualizadoEn = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Datos de SINPE actualizados correctamente.";
            return RedirectToAction("Index");
        }

        // Cliente: pantalla para ver el QR/datos SINPE y adjuntar su comprobante.
        [HttpGet]
        public async Task<IActionResult> SubirComprobante()
        {
            int? idUsuario = HttpContext.Session.GetInt32("ID");

            Cliente? cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.IdUsuario == idUsuario);

            if (cliente == null)
            {
                TempData["ErrorMessage"] = "Debe iniciar sesión como cliente.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Sinpe = await _context.ConfiguracionSinpe.FirstOrDefaultAsync();

            ViewBag.MembresiaActual = await _context.ClienteMembresia
                .Include(cm => cm.IdMembresiaNavigation)
                .Where(cm => cm.IdCliente == cliente.IdCliente)
                .OrderByDescending(cm => cm.FechaFin)
                .FirstOrDefaultAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubirComprobante(int idClienteMembresia, decimal monto, IFormFile comprobante)
        {
            int? idUsuario = HttpContext.Session.GetInt32("ID");

            Cliente? cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.IdUsuario == idUsuario);

            if (cliente == null)
            {
                TempData["ErrorMessage"] = "Debe iniciar sesión como cliente.";
                return RedirectToAction("Index", "Home");
            }

            bool perteneceAlCliente = await _context.ClienteMembresia.AnyAsync(cm =>
                cm.IdClienteMembresia == idClienteMembresia && cm.IdCliente == cliente.IdCliente);

            if (!perteneceAlCliente)
            {
                TempData["ErrorMessage"] = "La mensualidad indicada no le pertenece.";
                return RedirectToAction(nameof(SubirComprobante));
            }

            if (comprobante == null || comprobante.Length == 0)
            {
                TempData["ErrorMessage"] = "Debe adjuntar la imagen del comprobante.";
                return RedirectToAction(nameof(SubirComprobante));
            }

            string carpeta = Path.Combine(_env.WebRootPath, "img", "comprobantes");
            Directory.CreateDirectory(carpeta);

            string nombreArchivo = Guid.NewGuid().ToString("N") + Path.GetExtension(comprobante.FileName);
            string rutaFisica = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                await comprobante.CopyToAsync(stream);
            }

            Pago pago = new Pago
            {
                IdClienteMembresia = idClienteMembresia,
                Monto = monto,
                FechaPago = DateOnly.FromDateTime(DateTime.Today),
                MetodoPago = "SINPE",
                Descripcion = "Comprobante adjuntado por el cliente, pendiente de verificación.",
                ComprobantePago = "img/comprobantes/" + nombreArchivo
            };

            _context.Pagos.Add(pago);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comprobante enviado. El equipo lo verificará pronto.";
            return RedirectToAction(nameof(SubirComprobante));
        }
        #endregion
    }
}
