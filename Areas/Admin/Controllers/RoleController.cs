using Medycally.Core;
using Medycally.Core.Security;
using Medycally.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medycally.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class RoleController : Controller
    {
        private const string ModuleUrl = "/Admin/Role";

        private readonly ISecurityRole      _securityRole;
        private readonly IAdminUser         _adminUser;
        private readonly IPermissionService _permissions;

        public RoleController(ISecurityRole securityRole, IAdminUser adminUser, IPermissionService permissions)
        {
            _securityRole = securityRole;
            _adminUser    = adminUser;
            _permissions  = permissions;
        }

        public IActionResult Index()
        {
            var roles = _adminUser.GetAllRoles();
            return View(roles);
        }

        [HttpPost]
        public IActionResult Save([FromBody] SaveRoleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RoleName))
                return BadRequest(new { message = "El nombre del rol es requerido." });

            var required = request.SecurityRoleId == 0 ? PermissionAction.Create : PermissionAction.Edit;
            if (!_permissions.HasPermission(User, ModuleUrl, required))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso para realizar esta acción." });

            var role = new SecurityRoleModel
            {
                SecurityRoleId = request.SecurityRoleId,
                RoleName       = request.RoleName.Trim(),
                HasGlobalScope = request.HasGlobalScope
            };

            int id = _securityRole.AddOrEdit(role);

            foreach (var m in request.Modules ?? [])
                _securityRole.SaveModule(id, m);

            // Acciones nombradas por módulo (botones individuales)
            _securityRole.SaveActions(id, request.AllowedActionIds ?? []);

            return Ok(new { securityRoleId = id });
        }

        [HttpPost]
        [RequiresModulePermission(PermissionAction.Delete)]
        public IActionResult Delete([FromBody] int securityRoleId)
        {
            _securityRole.Delete(securityRoleId);
            return Ok();
        }

        [HttpGet]
        public IActionResult GetModules(int roleId)
        {
            var modules = _securityRole.GetModules(roleId);

            // Adjuntar acciones por módulo (catálogo completo + IsAllowed para el rol)
            var actions   = _securityRole.GetActionsByRole(roleId);
            var byModule  = actions.GroupBy(a => a.SecurityModuleId)
                                   .ToDictionary(g => g.Key, g => g.OrderBy(a => a.ActionOrder).ToList());
            foreach (var m in modules)
            {
                if (byModule.TryGetValue(m.SecurityModuleId, out var list))
                    m.Actions = list;
            }
            return Ok(modules);
        }
    }

    public class SaveRoleRequest
    {
        public int    SecurityRoleId { get; set; }
        public string RoleName       { get; set; } = string.Empty;
        public bool   HasGlobalScope { get; set; }
        public List<SecurityRoleModuleModel> Modules { get; set; } = [];

        // IDs (SecurityModuleActionId) de las acciones permitidas al rol
        public List<int> AllowedActionIds { get; set; } = [];
    }
}
