using Medycally.Core;
using Medycally.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medycally.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class ModuleController : Controller
    {
        private readonly IAdminModule _adminModule;

        public ModuleController(IAdminModule adminModule) => _adminModule = adminModule;

        public IActionResult Index()
        {
            if (!IsSuperAdmin()) return Forbid();
            var modules = _adminModule.GetAll();
            return View(modules);
        }

        [HttpPost]
        public IActionResult Save([FromBody] SecurityModuleAdminModel model)
        {
            if (!IsSuperAdmin()) return Forbid();

            if (string.IsNullOrWhiteSpace(model.ModuleName))
                return BadRequest(new { message = "El nombre del módulo es requerido." });

            model.ModuleName = model.ModuleName.Trim();
            model.ModuleUrl  = string.IsNullOrWhiteSpace(model.ModuleUrl)  ? null : model.ModuleUrl.Trim();
            model.ModuleIcon = string.IsNullOrWhiteSpace(model.ModuleIcon) ? null : model.ModuleIcon.Trim();

            if (model.ModuleUrl != null && !model.ModuleUrl.StartsWith('/'))
                return BadRequest(new { message = "La URL debe comenzar con '/'. Ej: /Admin/Reports" });

            if (model.ParentSecurityModuleId == 0) model.ParentSecurityModuleId = null;
            if (model.ModuleOrder < 0 || model.ModuleOrder > 255)
                return BadRequest(new { message = "El orden debe estar entre 0 y 255." });

            try
            {
                int id = _adminModule.AddOrEdit(model);
                return Ok(new { securityModuleId = id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult Delete([FromBody] int securityModuleId)
        {
            if (!IsSuperAdmin()) return Forbid();
            try
            {
                _adminModule.Delete(securityModuleId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private bool IsSuperAdmin()
            => string.Equals(User.FindFirst("IsSuperAdmin")?.Value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
