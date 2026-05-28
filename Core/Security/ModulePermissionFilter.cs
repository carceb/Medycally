using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Medycally.Core.Security
{
    /// <summary>
    /// Bloquea acceso por URL a páginas cubiertas por un SecurityModule cuando
    /// el rol del usuario no tiene CanView sobre esa fila.
    ///
    /// Una ruta /Area/Controller/Action se considera "gated" si coincide
    /// (igual o prefijo) con la ModuleUrl de algún módulo activo. Si lo está,
    /// el usuario debe tener acceso a ese módulo o se devuelve 403.
    /// Si la ruta no coincide con ninguna ModuleUrl, se permite (cubre AJAX
    /// y rutas que no están en el menú).
    /// </summary>
    public class ModulePermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly IPermissionService _permissions;

        public ModulePermissionFilter(IPermissionService permissions)
        {
            _permissions = permissions;
        }

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
                return Task.CompletedTask;

            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated != true)
                return Task.CompletedTask;

            if (string.Equals(user.FindFirst("IsSuperAdmin")?.Value, "true", StringComparison.OrdinalIgnoreCase))
                return Task.CompletedTask;

            var route      = context.RouteData.Values;
            var area       = route["area"]?.ToString();
            var controller = route["controller"]?.ToString();
            var action     = route["action"]?.ToString();
            if (string.IsNullOrEmpty(controller) || string.IsNullOrEmpty(action))
                return Task.CompletedTask;

            string requestScope = (string.IsNullOrEmpty(area) ? "" : "/" + area)
                                + "/" + controller
                                + "/" + action;

            var moduleUrl = _permissions.FindModuleUrl(requestScope);
            if (moduleUrl == null) return Task.CompletedTask;

            if (!_permissions.HasPermission(user, moduleUrl, PermissionAction.View))
            {
                context.Result = new ForbidResult();
            }

            return Task.CompletedTask;
        }
    }
}
