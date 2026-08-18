using G2_SC603_KN_Proyecto.Models;
using G2_SC603_KN_Proyecto.Helpers;
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
                .Where(cm => cm.FechaFin < ZonaHoraria.Hoy)
                .ToList();

            ViewBag.Sinpe = _context.ConfiguracionSinpe.FirstOrDefault();

            return View(pagos);
        }

        [HttpPost]
        public IActionResult RegistrarPago(Pago pago)
        {
            pago.EstadoVerificacion = "Verificado"; // lo registra el admin directamente
            _context.Pagos.Add(pago);
            _context.SaveChanges();

            GenerarNotificacionPago(pago.IdClienteMembresia, pago.Monto);

            return RedirectToAction("Index");
        }
        // Admin aprueba o rechaza un comprobante; al aprobar renueva la mensualidad 1 mes
        [HttpPost]
        public async Task<IActionResult> VerificarPago(int idPago, bool aprobado)
        {
            string rol = HttpContext.Session.GetString("Rol") ?? "";
            if (!rol.Contains("ADMIN") && !rol.Contains("RECEPTION"))
            {
                TempData["ErrorMessage"] = "No tiene permisos para esta acción.";
                return RedirectToAction("Index");
            }

            Pago? pago = await _context.Pagos
                .Include(p => p.IdClienteMembresiaNavigation)
                    .ThenInclude(cm => cm.IdMembresiaNavigation)
                .FirstOrDefaultAsync(p => p.IdPago == idPago);

            if (pago == null)
            {
                TempData["ErrorMessage"] = "No se encontró el pago.";
                return RedirectToAction("Index");
            }

            if (aprobado)
            {
                pago.EstadoVerificacion = "Verificado";

                ClienteMembresium membresiaCliente = pago.IdClienteMembresiaNavigation;
                DateOnly hoy = ZonaHoraria.Hoy;
                DateOnly baseFecha = membresiaCliente.FechaFin > hoy ? membresiaCliente.FechaFin : hoy;

                // Si pagó más de un mes de una vez (ej: ₡75,000 con mensualidad
                // de ₡25,000), se extienden los meses que efectivamente pagó,
                // no siempre 1 solo.
                decimal precioMensual = membresiaCliente.IdMembresiaNavigation?.Precio ?? pago.Monto;
                int meses = precioMensual > 0
                    ? Math.Max(1, (int)Math.Round(pago.Monto / precioMensual, MidpointRounding.AwayFromZero))
                    : 1;

                membresiaCliente.FechaFin = baseFecha.AddMonths(meses);
                membresiaCliente.Estado = "Activa";

                TempData["SuccessMessage"] = meses > 1
                    ? $"Pago verificado y mensualidad renovada por {meses} meses."
                    : "Pago verificado y mensualidad renovada.";
            }
            else
            {
                pago.EstadoVerificacion = "Rechazado";
                TempData["SuccessMessage"] = "Pago marcado como rechazado.";
            }

            await _context.SaveChangesAsync();

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
            if (!rol.Contains("ADMIN") && !rol.Contains("RECEPTION"))
            {
                TempData["ErrorMessage"] = "No tiene permisos para esta acción.";
                return RedirectToAction("Index");
            }

            if (imagen == null || imagen.Length == 0)
            {
                TempData["ErrorMessage"] = "Debe seleccionar una imagen.";
                return RedirectToAction("Index");
            }

            if (!EsImagenValida(imagen, out string errorImagen))
            {
                TempData["ErrorMessage"] = errorImagen;
                return RedirectToAction("Index");
            }

            string carpeta = Path.Combine(_env.WebRootPath, "img", "sinpe");
            Directory.CreateDirectory(carpeta);

            string nombreArchivo = Guid.NewGuid().ToString("N") + Path.GetExtension(imagen.FileName).ToLower();
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

            ClienteMembresium? membresiaActual = await _context.ClienteMembresia
                .Include(cm => cm.IdMembresiaNavigation)
                .Where(cm => cm.IdCliente == cliente.IdCliente)
                .OrderByDescending(cm => cm.FechaFin)
                .FirstOrDefaultAsync();

            ViewBag.MembresiaActual = membresiaActual;

            DateOnly hoy = ZonaHoraria.Hoy;

            // No permite otro envío mientras haya uno pendiente de revisión
            Pago? pagoPendiente = await _context.Pagos
                .Include(p => p.IdClienteMembresiaNavigation)
                .Where(p => p.IdClienteMembresiaNavigation.IdCliente == cliente.IdCliente
                    && p.EstadoVerificacion == "Pendiente")
                .OrderByDescending(p => p.FechaPago)
                .FirstOrDefaultAsync();

            ViewBag.PagoPendiente = pagoPendiente;

            // Mensualidad vigente con más de 5 días de margen: no hace falta pagar de nuevo
            bool mensualidadAlDia = membresiaActual != null
                && membresiaActual.Estado == "Activa"
                && membresiaActual.FechaFin > hoy.AddDays(5);

            ViewBag.MensualidadAlDia = mensualidadAlDia;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubirComprobante(int idClienteMembresia, int meses, IFormFile comprobante)
        {
            int? idUsuario = HttpContext.Session.GetInt32("ID");

            Cliente? cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.IdUsuario == idUsuario);

            if (cliente == null)
            {
                TempData["ErrorMessage"] = "Debe iniciar sesión como cliente.";
                return RedirectToAction("Index", "Home");
            }

            // El monto NUNCA se toma del formulario: se recalcula acá con el
            // precio vigente de la mensualidad, para que no pueda quedar
            // desalineado con lo que después usa VerificarPago para calcular
            // cuántos meses corresponden.
            ClienteMembresium? membresiaCliente = await _context.ClienteMembresia
                .Include(cm => cm.IdMembresiaNavigation)
                .FirstOrDefaultAsync(cm =>
                    cm.IdClienteMembresia == idClienteMembresia && cm.IdCliente == cliente.IdCliente);

            if (membresiaCliente == null)
            {
                TempData["ErrorMessage"] = "La mensualidad indicada no le pertenece.";
                return RedirectToAction(nameof(SubirComprobante));
            }

            if (meses < 1) meses = 1;
            if (meses > 12) meses = 12;

            decimal monto = (membresiaCliente.IdMembresiaNavigation?.Precio ?? 0) * meses;

            bool yaTienePendiente = await _context.Pagos
                .Include(p => p.IdClienteMembresiaNavigation)
                .AnyAsync(p => p.IdClienteMembresiaNavigation.IdCliente == cliente.IdCliente
                    && p.EstadoVerificacion == "Pendiente");

            if (yaTienePendiente)
            {
                TempData["ErrorMessage"] = "Ya tenés un comprobante en revisión, esperá a que el equipo lo confirme.";
                return RedirectToAction(nameof(SubirComprobante));
            }

            if (comprobante == null || comprobante.Length == 0)
            {
                TempData["ErrorMessage"] = "Debe adjuntar la imagen del comprobante.";
                return RedirectToAction(nameof(SubirComprobante));
            }

            if (!EsImagenValida(comprobante, out string errorComprobante))
            {
                TempData["ErrorMessage"] = errorComprobante;
                return RedirectToAction(nameof(SubirComprobante));
            }

            string carpeta = Path.Combine(_env.WebRootPath, "img", "comprobantes");
            Directory.CreateDirectory(carpeta);

            string nombreArchivo = Guid.NewGuid().ToString("N") + Path.GetExtension(comprobante.FileName).ToLower();
            string rutaFisica = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                await comprobante.CopyToAsync(stream);
            }

            Pago pago = new Pago
            {
                IdClienteMembresia = idClienteMembresia,
                Monto = monto,
                FechaPago = ZonaHoraria.Hoy,
                MetodoPago = "SINPE",
                Descripcion = meses > 1
                    ? $"Comprobante adjuntado por el cliente ({meses} meses), pendiente de verificación."
                    : "Comprobante adjuntado por el cliente, pendiente de verificación.",
                ComprobantePago = "img/comprobantes/" + nombreArchivo,
                EstadoVerificacion = "Pendiente"
            };

            _context.Pagos.Add(pago);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comprobante enviado. El equipo lo verificará pronto.";
            return RedirectToAction(nameof(SubirComprobante));
        }

        // Cliente elige pagar en efectivo al llegar; queda Pendiente igual que un SINPE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PagarEnEfectivo(int idClienteMembresia, int meses)
        {
            int? idUsuario = HttpContext.Session.GetInt32("ID");

            Cliente? cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.IdUsuario == idUsuario);

            if (cliente == null)
            {
                TempData["ErrorMessage"] = "Debe iniciar sesión como cliente.";
                return RedirectToAction("Index", "Home");
            }

            ClienteMembresium? membresiaCliente = await _context.ClienteMembresia
                .Include(cm => cm.IdMembresiaNavigation)
                .FirstOrDefaultAsync(cm =>
                    cm.IdClienteMembresia == idClienteMembresia && cm.IdCliente == cliente.IdCliente);

            if (membresiaCliente == null)
            {
                TempData["ErrorMessage"] = "La mensualidad indicada no le pertenece.";
                return RedirectToAction(nameof(SubirComprobante));
            }

            bool yaTienePendiente = await _context.Pagos
                .Include(p => p.IdClienteMembresiaNavigation)
                .AnyAsync(p => p.IdClienteMembresiaNavigation.IdCliente == cliente.IdCliente
                    && p.EstadoVerificacion == "Pendiente");

            if (yaTienePendiente)
            {
                TempData["ErrorMessage"] = "Ya tenés un pago en revisión, esperá a que el equipo lo confirme.";
                return RedirectToAction(nameof(SubirComprobante));
            }

            if (meses < 1) meses = 1;
            if (meses > 12) meses = 12;

            decimal monto = (membresiaCliente.IdMembresiaNavigation?.Precio ?? 0) * meses;

            Pago pago = new Pago
            {
                IdClienteMembresia = idClienteMembresia,
                Monto = monto,
                FechaPago = ZonaHoraria.Hoy,
                MetodoPago = "Efectivo",
                Descripcion = meses > 1
                    ? $"El cliente eligió pagar en efectivo al llegar al gimnasio ({meses} meses), pendiente de cobro."
                    : "El cliente eligió pagar en efectivo al llegar al gimnasio, pendiente de cobro.",
                EstadoVerificacion = "Pendiente"
            };

            _context.Pagos.Add(pago);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Listo, quedó registrado que vas a pagar en efectivo al llegar.";
            return RedirectToAction(nameof(SubirComprobante));
        }
        #endregion

        // Valida que el archivo subido sea realmente una imagen: revisa la
        // extensión, el tamaño máximo, y la "firma" de los primeros bytes
        // del archivo (no alcanza con confiar en el nombre/extensión, que
        // se puede falsificar fácilmente).
        private static bool EsImagenValida(IFormFile archivo, out string error)
        {
            const long tamanoMaximoBytes = 5 * 1024 * 1024; // 5 MB

            string extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            string[] extensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };

            if (!extensionesPermitidas.Contains(extension))
            {
                error = "El archivo debe ser una imagen (jpg, png o webp).";
                return false;
            }

            if (archivo.Length > tamanoMaximoBytes)
            {
                error = "La imagen no puede pesar más de 5 MB.";
                return false;
            }

            byte[] encabezado = new byte[12];
            using (var stream = archivo.OpenReadStream())
            {
                stream.Read(encabezado, 0, encabezado.Length);
            }

            bool esJpeg = encabezado[0] == 0xFF && encabezado[1] == 0xD8 && encabezado[2] == 0xFF;
            bool esPng = encabezado[0] == 0x89 && encabezado[1] == 0x50 && encabezado[2] == 0x4E && encabezado[3] == 0x47;
            bool esWebp = encabezado[0] == 0x52 && encabezado[1] == 0x49 && encabezado[2] == 0x46 && encabezado[3] == 0x46
                && encabezado[8] == 0x57 && encabezado[9] == 0x45 && encabezado[10] == 0x42 && encabezado[11] == 0x50;

            if (!esJpeg && !esPng && !esWebp)
            {
                error = "El archivo no es una imagen válida (el contenido no coincide con la extensión).";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
