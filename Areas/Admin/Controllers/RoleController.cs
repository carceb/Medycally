using Medycally.Core;
using Medycally.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medycally.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class RoleController : Controller
    {
        private readonly ISecurityRole _securityRole;
        private readonly IAdminUser    _adminUser;

        public RoleController(ISecurityRole securityRole, IAdminUser adminUser)
        {
            _securityRole = securityRole;
            _adminUser    = adminUser;
        }

        public IActionResult Index()
        {
            if (!IsSuperAdmin()) return Forbid();
            var roles = _adminUser.GetAllRoles();
            return View(roles);
        }

        [HttpPost]
        public IActionResult Save([FromBody] SaveRoleRequest request)
        {
            if (!IsSuperAdmin()) return Forbid();

            if (string.IsNullOrWhiteSpace(request.RoleName))
                return BadRequest(new { message = "El nombre del rol es requerido." });

            var role = new SecurityRoleModel
            {
                SecurityRoleId = request.SecurityRoleId,
                RoleName       = request.RoleName.Trim()
            };

            int id = _securityRole.AddOrEdit(role);

            foreach (var m in request.Modules ?? [])
                _securityRole.SaveModule(id, m);

            return Ok(new { securityRoleId = id });
        }

        [HttpPost]
        public IActionResult Delete([FromBody] int securityRoleId)
        {
            if (!IsSuperAdmin()) return Forbid();
            _securityRole.Delete(securityRoleId);
            return Ok();
        }

        [HttpGet]
        public IActionResult GetModules(int roleId)
        {
            if (!IsSuperAdmin()) return Forbid();
            var modules = _securityRole.GetModules(roleId);
            return Ok(modules);
        }

        private bool IsSuperAdmin()
            => string.Equals(User.FindFirst("IsSuperAdmin")?.Value, "true", StringComparison.OrdinalIgnoreCase);
    }

    public class SaveRoleRequest
    {
        public int    SecurityRoleId { get; set; }
        public string RoleName       { get; set; } = string.Empty;
        public List<SecurityRoleModuleModel> Modules { get; set; } = [];
    }
}
