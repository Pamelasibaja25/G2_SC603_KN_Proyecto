using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;

namespace G2_SC603_KN_Proyecto.Filters
{
    /// Filtro de autorización basado en el rol guardado en sesión
    /// (el proyecto no usa ASP.NET Identity, así que [Authorize] no aplica).
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RolAutorizadoAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string[] _rolesPermitidos;

        public RolAutorizadoAttribute(params string[] rolesPermitidos)
        {
            _rolesPermitidos = rolesPermitidos;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            string rolActual = context.HttpContext.Session.GetString("Rol") ?? string.Empty;

            // rolActual puede ser multi-rol ("ADMIN,TRAINER"): alcanza con que
            // el usuario tenga AL MENOS UNO de los roles permitidos.
            bool autorizado = _rolesPermitidos.Any(r => rolActual.Contains(r));

            if (!autorizado)
            {
                if (context.Controller is Controller controller)
                {
                    controller.TempData["ErrorMessage"] = "No tiene permisos para realizar esta acción.";
                }

                context.Result = new RedirectToActionResult("Home", "Home", null);
                return;
            }

            await next();
        }
    }
}
