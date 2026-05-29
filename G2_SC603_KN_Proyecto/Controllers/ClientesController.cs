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

        #region Mostrar Clientes
        public async Task<IActionResult> MostrarClientes()
        {
            /*var clientes = await _context.Clientes
                .Include(c => c.IdUsuarioNavigation)
                .ToListAsync();*/

            var clientes = await _context.ClientesResumen
        .FromSqlRaw("CALL sp_ObtenerClientesResumen()")
        .ToListAsync();

            return View(clientes);
        }
        #endregion

        #region AgregarCliente
        [HttpPost]
        public async Task<IActionResult> AgregarCliente(ClienteResumen nuevoCliente)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                "CALL sp_AgregarCliente({0}, {1}, {2}, {3}, {4}, {5})",
                nuevoCliente.Nombre,
                nuevoCliente.Cedula,
                nuevoCliente.Telefono,
                nuevoCliente.Correo,
                nuevoCliente.fecha_nacimiento,
                nuevoCliente.Estado
            );
                TempData["SuccessMessage"] = "Cliente agregado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al agregar el cliente: " + ex.Message;
            }

            return RedirectToAction("MostrarClientes");
        }
        #endregion
    }
}
