using G2_SC603_KN_Proyecto.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G2_SC603_KN_Proyecto.Controllers
{
    public class ClientesController : Controller
    {
        private readonly DbOrionFitContext _context;

        public ClientesController(DbOrionFitContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> MostrarClientes()
        {
            var clientes = await _context.Clientes
                .Include(c => c.IdUsuarioNavigation)
                .ToListAsync();

            return View(clientes);
        }
    }
}
