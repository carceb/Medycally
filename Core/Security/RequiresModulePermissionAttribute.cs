using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Medycally.Core.Security
{
    /// <summary>
    /// Bloquea una acción si el rol del usuario no tiene el permiso requerido
    /// sobre el módulo asociado. El módulo se determina así:
    ///   - Si <see cref="ModuleUrl"/> está definido, se usa esa URL.
    ///   - Si no, se deriva de la ruta actual buscando el ModuleUrl registrado
    ///     que mejor coincida con /Area/Controller/Index del request.
    /// Si la acción no está cubierta por ningún módulo registrado, se permite.
    /// SuperAdmin siempre pasa.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RequiresModulePermissionAttribute : Attribute, IFilterFactory
    {
        public PermissionAction Action { get; }
        public string?          ModuleUrl { get; }

        public RequiresModulePermissionAttribute(PermissionAction action, string? moduleUrl = null)
        {
            Action    = action;
            ModuleUrl = moduleUrl;
        }

        public bool IsReusable => false;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var svc = serviceProvider.GetRequiredService<IPermissionService>();
            return new RequiresModulePermissionFilter(svc, Action, ModuleUrl);
        }
    }

    internal sealed class RequiresModulePermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly IPermissionService _service;
        private readonly PermissionAction   _action;
        private readonly string?            _explicitModuleUrl;

        public RequiresModulePermissionFilter(IPermissionService service, PermissionAction action, string? explicitModuleUrl)
        {
            _service           = service;
            _action            = action;
            _explicitModuleUrl = explicitModuleUrl;
        }

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = new ForbidResult();
                return Task.CompletedTask;
            }

            if (string.Equals(user.FindFirst("IsSuperAdmin")?.Value, "true", StringComparison.OrdinalIgnoreCase))
                return Task.CompletedTask;

            string? moduleUrl = _explicitModuleUrl;
            if (string.IsNullOrEmpty(moduleUrl))
            {
                var route      = context.RouteData.Values;
                var area       = route["area"]?.ToString();
                var controller = route["controller"]?.ToString();
                if (string.IsNullOrEmpty(controller)) return Task.CompletedTask;

                // Buscamos por /Area/Controller/Index — el módulo siempre apunta a la landing del controlador.
                string controllerIndex = (string.IsNullOrEmpty(area) ? "" : "/" + area)
                                       + "/" + controller + "/Index";
                moduleUrl = _service.FindModuleUrl(controllerIndex);
            }

            if (string.IsNullOrEmpty(moduleUrl))
                return Task.CompletedTask; // No gated module → permitido

            if (!_service.HasPermission(user, moduleUrl, _action))
            {
                context.Result = new ObjectResult(new { message = "No tienes permiso para realizar esta acción." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            return Task.CompletedTask;
        }
    }
}
