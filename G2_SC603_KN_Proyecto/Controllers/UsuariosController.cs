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
        .FromSqlRaw("CALL sp_obtenerUsuariosConNombre()")
        .ToListAsync();

            // La SP no trae "activo"; se completa con una consulta aparte.
            var estadosActivos = await _context.Usuarios
                .Select(u => new { u.Username, u.Activo })
                .ToDictionaryAsync(u => u.Username, u => u.Activo);

            foreach (var usuario in Usuarios)
            {
                usuario.Activo = !estadosActivos.TryGetValue(usuario.Username, out bool activo) || activo;
            }

            return View(Usuarios);
        }
        #endregion

        #region Activar / Desactivar Usuario
        [HttpPost]
        public async Task<IActionResult> ToggleActivoUsuario(string username)
        {
            string usuarioSesion = HttpContext.Session.GetString("Usuario") ?? "";
            if (string.Equals(usuarioSesion, username, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "No podés desactivar tu propia cuenta.";
                return RedirectToAction("MostrarUsuarios");
            }

            Usuario? usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == username);
            if (usuario == null)
            {
                TempData["ErrorMessage"] = "Usuario no encontrado.";
                return RedirectToAction("MostrarUsuarios");
            }

            usuario.Activo = !usuario.Activo;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = usuario.Activo
                ? "Usuario activado correctamente."
                : "Usuario desactivado. Ya no va a poder iniciar sesión.";

            return RedirectToAction("MostrarUsuarios");
        }
        #endregion

        #region Eliminar Usuario
        [HttpPost]
        public async Task<IActionResult> EliminarUsuario(string username)
        {
            Usuario? usuario = await _context.Usuarios
                .Include(u => u.Administradors)
                .Include(u => u.Entrenadors)
                .Include(u => u.Clientes)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (usuario == null)
            {
                TempData["ErrorMessage"] = "Usuario no encontrado.";
                return RedirectToAction("MostrarUsuarios");
            }

            if (usuario.Activo)
            {
                TempData["ErrorMessage"] = "Solo se pueden eliminar usuarios desactivados.";
                return RedirectToAction("MostrarUsuarios");
            }

            if (usuario.Clientes.Any())
            {
                TempData["ErrorMessage"] = "Esta cuenta pertenece a un cliente: eliminala desde Clientes, no desde acá.";
                return RedirectToAction("MostrarUsuarios");
            }

            _context.Administradors.RemoveRange(usuario.Administradors);
            _context.Entrenadors.RemoveRange(usuario.Entrenadors);
            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Usuario eliminado correctamente.";
            return RedirectToAction("MostrarUsuarios");
        }
        #endregion

        #region Agregar Usuario
        [HttpPost]
        public async Task<IActionResult> AgregarUsuario(UsuarioNombre nuevoUsuario, List<string> roles)
        {
            try
            {
                string rolCsv = roles != null && roles.Any()
                    ? string.Join(",", roles.Distinct())
                    : nuevoUsuario.Rol; // fallback por si el form no manda la lista

                await _context.Database.ExecuteSqlRawAsync(
                "CALL sp_agregarUsuario({0}, {1}, {2}, {3}, {4})",
                nuevoUsuario.Nombre,
                nuevoUsuario.Telefono,
                nuevoUsuario.Correo,
                rolCsv,
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
        public async Task<IActionResult> EditarUsuario(UsuarioNombre nuevoUsuario, List<string> roles)
        {
            try
            {
                string rolCsv = roles != null && roles.Any()
                    ? string.Join(",", roles.Distinct())
                    : nuevoUsuario.Rol;

                await _context.Database.ExecuteSqlRawAsync(
                "CALL sp_editarUsuario({0}, {1}, {2}, {3}, {4})",
                nuevoUsuario.Nombre,
                nuevoUsuario.Telefono,
                nuevoUsuario.Correo,
                rolCsv,
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


        #region Configuracion
        public async Task<IActionResult> Configuracion()
        {
            var idUsuario = HttpContext.Session.GetInt32("ID");
            var usuarios = await _context.UsuarioNombre
    .FromSqlRaw("CALL sp_obtenerUsuarioConNombre({0})", idUsuario)
    .ToListAsync();

            var usuario = usuarios.FirstOrDefault();


            return View(usuario);
        }
        #endregion

        #region Cambiar Contraseña
        [HttpPost]
        public async Task<IActionResult> CambiarContraseña(IFormCollection form)
        {
            try
            {
                var idUsuario = HttpContext.Session.GetInt32("ID");
                var passwordActual = form["PasswordActual"].ToString().Trim();
                var passwordNueva = form["PasswordNueva"].ToString().Trim();
                await _context.Database.ExecuteSqlRawAsync(
                "CALL sp_actualizarContraseña({0}, {1}, {2})",
                idUsuario,
                passwordActual,
                passwordNueva
            );
                TempData["SuccessMessage"] = "Contraseña editada correctamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al editar la contraseña: " + ex.Message;
            }

            return RedirectToAction("Configuracion");
        }
        #endregion

        #region Cambiar Datos
        [HttpPost]
        public async Task<IActionResult> CambiarDatos(UsuarioNombre nuevoUsuario)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                "CALL sp_editarUsuario({0}, {1}, {2}, {3}, {4})",
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

            return RedirectToAction("Configuracion");
        }
        #endregion
    }
}
