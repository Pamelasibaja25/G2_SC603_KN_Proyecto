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

        #region Agregar Usuario
        [HttpPost]
        public async Task<IActionResult> AgregarUsuario(UsuarioNombre nuevoUsuario)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                "CALL sp_AgregarUsuario({0}, {1}, {2}, {3}, {4})",
                nuevoUsuario.Nombre,
                nuevoUsuario.Telefono,
                nuevoUsuario.Correo,
                nuevoUsuario.Rol,
                nuevoUsuario.Username
            );
                TempData["SuccessMessage"] = "Usuario agregado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al agregar el Usuario: " + ex.Message;
            }

            return RedirectToAction("MostrarUsuarios");
        }
        #endregion

        #region Editar Usuario
        [HttpPost]
        public async Task<IActionResult> EditarUsuario(UsuarioNombre nuevoUsuario)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                "CALL sp_EditarUsuario({0}, {1}, {2}, {3}, {4})",
                nuevoUsuario.Nombre,
                nuevoUsuario.Telefono,
                nuevoUsuario.Correo,
                nuevoUsuario.Rol,
                nuevoUsuario.Username
            );
                TempData["SuccessMessage"] = "Usuario editado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al editar el Usuario: " + ex.Message;
            }

            return RedirectToAction("MostrarUsuarios");
        }
        #endregion
    }
}
