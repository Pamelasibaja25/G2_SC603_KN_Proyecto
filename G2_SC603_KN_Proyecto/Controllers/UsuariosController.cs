using G2_SC603_KN_Proyecto.Models;
using G2_SC603_KN_Proyecto.Filters;
using G2_SC603_KN_Proyecto.Helpers;
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
        [HttpGet]
        [RolAutorizado("ADMIN")]
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
        [RolAutorizado("ADMIN")]
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
        [RolAutorizado("ADMIN")]
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
        [RolAutorizado("ADMIN")]
        public async Task<IActionResult> AgregarUsuario(UsuarioNombre nuevoUsuario, List<string> roles, string contrasena)
        {
            string username = (nuevoUsuario.Username ?? "").Trim();
            if (!Regex.IsMatch(username, "^[A-Za-z0-9]{3,30}$"))
            {
                TempData["ErrorMessage"] = "El usuario debe tener entre 3 y 30 caracteres, solo letras y números (sin guiones, puntos ni espacios).";
                return RedirectToAction("MostrarUsuarios");
            }

            if (string.IsNullOrWhiteSpace(contrasena) || contrasena.Trim().Length < 6)
            {
                TempData["ErrorMessage"] = "La contraseña inicial debe tener al menos 6 caracteres.";
                return RedirectToAction("MostrarUsuarios");
            }

            if (roles == null || !roles.Any())
            {
                TempData["ErrorMessage"] = "Debe seleccionar al menos un rol.";
                return RedirectToAction("MostrarUsuarios");
            }

            try
            {
                string rolCsv = string.Join(",", roles.Distinct());
                string hashContrasena = PasswordHasher.HashPassword(contrasena.Trim());

                await _context.Database.ExecuteSqlRawAsync(
                "CALL sp_agregarUsuario({0}, {1}, {2}, {3}, {4}, {5})",
                nuevoUsuario.Nombre,
                nuevoUsuario.Telefono,
                nuevoUsuario.Correo,
                rolCsv,
                username,
                hashContrasena
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
        [RolAutorizado("ADMIN")]
        public async Task<IActionResult> EditarUsuario(UsuarioNombre nuevoUsuario, List<string> roles)
        {
            if (roles == null || !roles.Any())
            {
                TempData["ErrorMessage"] = "Debe seleccionar al menos un rol.";
                return RedirectToAction("MostrarUsuarios");
            }

            try
            {
                string rolCsv = string.Join(",", roles.Distinct());

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

            Usuario? usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);
            if (usuario == null || !PasswordHasher.Verify(passwordActual, usuario.Contrasena, out _))
            {
                TempData["ErrorMessage"] = "Error al editar la contraseña: la contraseña actual no es correcta.";
                return RedirectToAction("Configuracion");
            }

            usuario.Contrasena = PasswordHasher.HashPassword(passwordNueva);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Contraseña editada correctamente.";
            return RedirectToAction("Configuracion");
        }
        #endregion

        #region Cambiar Datos
        // Autoservicio: el usuario logueado edita SUS PROPIOS datos de
        // contacto. El username objetivo y el rol NUNCA se toman del
        // formulario — se resuelven siempre desde la sesión / la base de
        // datos, para que nadie pueda mandar un username distinto al suyo
        // ni un rol distinto al que ya tiene (antes esta acción ni
        // siquiera exigía sesión iniciada, y aceptaba username y rol
        // arbitrarios enviados en el formulario: cualquiera podía editar
        // la cuenta de cualquier otra persona, incluyendo asignarse ADMIN).
        [HttpPost]
        public async Task<IActionResult> CambiarDatos(UsuarioNombre nuevoUsuario)
        {
            int? idUsuario = HttpContext.Session.GetInt32("ID");
            if (idUsuario == null)
            {
                return RedirectToAction("Index", "Home");
            }

            Usuario? usuarioSesion = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

            if (usuarioSesion == null)
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                "CALL sp_editarUsuario({0}, {1}, {2}, {3}, {4})",
                nuevoUsuario.Nombre,
                nuevoUsuario.Telefono,
                nuevoUsuario.Correo,
                usuarioSesion.Rol,
                usuarioSesion.Username
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
