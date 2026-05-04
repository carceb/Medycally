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
        public IActionResult Save([FromBody] SecurityRoleModel model)
        {
            if (!IsSuperAdmin()) return Forbid();

            if (string.IsNullOrWhiteSpace(model.RoleName))
                return BadRequest(new { message = "El nombre del rol es requerido." });

            if (model.RoleLevel < 1 || model.RoleLevel > 10)
                return BadRequest(new { message = "El nivel debe estar entre 1 y 10." });

            var id = _securityRole.AddOrEdit(model);
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

        [HttpPost]
        public IActionResult SaveModules([FromBody] SaveModulesRequest request)
        {
            if (!IsSuperAdmin()) return Forbid();

            foreach (var m in request.Modules)
                _securityRole.SaveModule(request.SecurityRoleId, m);

            return Ok();
        }

        private bool IsSuperAdmin()
        {
            var level = User.FindFirst("RoleLevel")?.Value;
            return level == "1";
        }
    }

    public class SaveModulesRequest
    {
        public int SecurityRoleId { get; set; }
        public List<SecurityRoleModuleModel> Modules { get; set; } = [];
    }
}
