using System.Security.Claims;
using Medycally.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Medycally.Core.Security
{
    public enum PermissionAction { View, Create, Edit, Delete }

    public sealed record ActionPermissions(bool CanView, bool CanCreate, bool CanEdit, bool CanDelete)
    {
        public static readonly ActionPermissions Full = new(true, true, true, true);
        public static readonly ActionPermissions None = new(false, false, false, false);

        public bool Has(PermissionAction action) => action switch
        {
            PermissionAction.View   => CanView,
            PermissionAction.Create => CanCreate,
            PermissionAction.Edit   => CanEdit,
            PermissionAction.Delete => CanDelete,
            _ => false
        };
    }

    public interface IPermissionService
    {
        ActionPermissions GetPermissions(ClaimsPrincipal user, string moduleUrl);
        bool HasPermission(ClaimsPrincipal user, string moduleUrl, PermissionAction action);

        /// <summary>
        /// Encuentra el ModuleUrl registrado que mejor coincide con la ruta dada
        /// (exact o prefijo). Devuelve el más específico (más largo). null = no gated.
        /// </summary>
        string? FindModuleUrl(string requestScope);

        /// <summary>
        /// Indica si el rol del usuario tiene asignada la acción nombrada dentro
        /// del módulo (ej. "doctors" en /Admin/Clinic). SuperAdmin siempre true.
        /// </summary>
        bool HasModuleAction(ClaimsPrincipal user, string moduleUrl, string actionKey);

        /// <summary>
        /// Devuelve el conjunto de ActionKeys permitidas al usuario para ese módulo.
        /// SuperAdmin obtiene todas las que su rol tenga asignadas (por seed → todas).
        /// </summary>
        HashSet<string> GetModuleActions(ClaimsPrincipal user, string moduleUrl);
    }

    public class PermissionService : IPermissionService
    {
        private static readonly TimeSpan UserPermsTtl = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan AllUrlsTtl   = TimeSpan.FromMinutes(10);

        private readonly ISecurityModule _securityModule;
        private readonly ISecurityRole   _securityRole;
        private readonly IMemoryCache    _cache;

        public PermissionService(ISecurityModule securityModule, ISecurityRole securityRole, IMemoryCache cache)
        {
            _securityModule = securityModule;
            _securityRole   = securityRole;
            _cache          = cache;
        }

        public ActionPermissions GetPermissions(ClaimsPrincipal user, string moduleUrl)
        {
            if (user?.Identity?.IsAuthenticated != true) return ActionPermissions.None;

            if (string.Equals(user.FindFirst("IsSuperAdmin")?.Value, "true", StringComparison.OrdinalIgnoreCase))
                return ActionPermissions.Full;

            if (!int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                return ActionPermissions.None;

            // Sin cache para garantizar que los cambios en SecurityRoleModule se reflejen
            // inmediatamente. El SP es muy ligero (un INNER JOIN sobre tablas pequeñas).
            var perms = _securityModule.GetUserPermissions(userId);
            var match = perms.FirstOrDefault(m =>
                !string.IsNullOrEmpty(m.ModuleUrl) && IsScopeMatch(moduleUrl, m.ModuleUrl));
            if (match == null) return ActionPermissions.None;
            return new ActionPermissions(true, match.CanCreate, match.CanEdit, match.CanDelete);
        }

        public bool HasPermission(ClaimsPrincipal user, string moduleUrl, PermissionAction action)
            => GetPermissions(user, moduleUrl).Has(action);

        public bool HasModuleAction(ClaimsPrincipal user, string moduleUrl, string actionKey)
        {
            if (user?.Identity?.IsAuthenticated != true) return false;
            if (string.Equals(user.FindFirst("IsSuperAdmin")?.Value, "true", StringComparison.OrdinalIgnoreCase))
                return true;
            return GetModuleActions(user, moduleUrl).Contains(actionKey);
        }

        public HashSet<string> GetModuleActions(ClaimsPrincipal user, string moduleUrl)
        {
            if (user?.Identity?.IsAuthenticated != true) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Sin cache intencionalmente: un SELECT con dos JOINs por request es barato
            // y evita problemas de staleness cuando un admin cambia las acciones de un
            // rol mientras hay usuarios activos. Si crece la carga, considerar invalidar
            // por usuario al editar el rol.
            var actions = _securityRole.GetUserActions(userId, moduleUrl) ?? new List<string>();
            return new HashSet<string>(actions, StringComparer.OrdinalIgnoreCase);
        }

        public string? FindModuleUrl(string requestScope)
        {
            var allUrls = LoadAllUrls();
            return allUrls
                .Where(u => IsScopeMatch(requestScope, u))
                .OrderByDescending(u => u.Length)
                .FirstOrDefault();
        }

        private List<NavigationModuleModel> LoadUserPermissions(int userId)
            => _cache.GetOrCreate($"perms_{userId}", entry =>
            {
                entry.SlidingExpiration = UserPermsTtl;
                return _securityModule.GetUserPermissions(userId);
            }) ?? [];

        private List<string> LoadAllUrls()
            => _cache.GetOrCreate("all_module_urls", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = AllUrlsTtl;
                return _securityModule.GetAllActiveModuleUrls();
            }) ?? [];

        private static bool IsScopeMatch(string requestScope, string moduleUrl)
            => requestScope.Equals(moduleUrl, StringComparison.OrdinalIgnoreCase)
            || requestScope.StartsWith(moduleUrl + "/", StringComparison.OrdinalIgnoreCase);
    }
}
