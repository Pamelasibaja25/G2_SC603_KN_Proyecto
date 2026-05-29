using G2_SC603_KN_Proyecto.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G2_SC603_KN_Proyecto.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly DbOrionFitContext _context;

        public UsuariosController(DbOrionFitContext context)
        {
            _context = context;
        }

        #region Mostrar Usuarios
        public async Task<IActionResult> MostrarUsuarios()
        {

            var Usuarios = await _context.UsuarioNombre
        .FromSqlRaw("CALL sp_ObtenerUsuariosConNombre()")
        .ToListAsync();

            return View(Usuarios);
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

            return RedirectToAction("MostrarUsuarios");
        }
        #endregion
    }
}
