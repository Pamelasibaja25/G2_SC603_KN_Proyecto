using G2_SC603_KN_Proyecto.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace G2_SC603_KN_Proyecto.Controllers
{
    public class AnunciosController : Controller
    {
        private readonly DbOrionFitContext _context;

        public AnunciosController(DbOrionFitContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var anuncios = _context.Anuncios.ToList();
            return View(anuncios);
        }

        [HttpPost]
        public IActionResult Crear(Anuncio anuncio)
        {
            _context.Anuncios.Add(anuncio);
            _context.SaveChanges();

            GenerarNotificacionAnuncio(anuncio);

            return RedirectToAction("Index");
        }

        private void GenerarNotificacionAnuncio(Anuncio anuncio)
        {
            var clientes = _context.Clientes.ToList();

            foreach (var cliente in clientes)
            {
                _context.Notificaciones.Add(new Notificacion
                {
                    IdCliente = cliente.IdCliente,
                    Tipo = "Anuncio",
                    Titulo = anuncio.Titulo,
                    Mensaje = anuncio.Mensaje,
                    Fecha = DateTime.Now,
                    Leida = false
                });
            }

            _context.SaveChanges();
        }
    }
}