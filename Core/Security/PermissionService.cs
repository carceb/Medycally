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
    }

    public class PermissionService : IPermissionService
    {
        private static readonly TimeSpan UserPermsTtl = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan AllUrlsTtl   = TimeSpan.FromMinutes(10);

        private readonly ISecurityModule _securityModule;
        private readonly IMemoryCache    _cache;

        public PermissionService(ISecurityModule securityModule, IMemoryCache cache)
        {
            _securityModule = securityModule;
            _cache          = cache;
        }

        public ActionPermissions GetPermissions(ClaimsPrincipal user, string moduleUrl)
        {
            if (user?.Identity?.IsAuthenticated != true) return ActionPermissions.None;

            if (string.Equals(user.FindFirst("IsSuperAdmin")?.Value, "true", StringComparison.OrdinalIgnoreCase))
                return ActionPermissions.Full;

            if (!int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                return ActionPermissions.None;

            var perms = LoadUserPermissions(userId);
            var match = perms.FirstOrDefault(m =>
                !string.IsNullOrEmpty(m.ModuleUrl) && IsScopeMatch(moduleUrl, m.ModuleUrl));
            if (match == null) return ActionPermissions.None;
            return new ActionPermissions(true, match.CanCreate, match.CanEdit, match.CanDelete);
        }

        public bool HasPermission(ClaimsPrincipal user, string moduleUrl, PermissionAction action)
            => GetPermissions(user, moduleUrl).Has(action);

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
