using Medycally.Core;
using Medycally.Core.Security;
using Medycally.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medycally.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class ModuleController : Controller
    {
        private const string ModuleUrl = "/Admin/Module";

        private readonly IAdminModule       _adminModule;
        private readonly IPermissionService _permissions;

        public ModuleController(IAdminModule adminModule, IPermissionService permissions)
        {
            _adminModule = adminModule;
            _permissions = permissions;
        }

        public IActionResult Index()
        {
            var modules = _adminModule.GetAll();
            return View(modules);
        }

        [HttpPost]
        public IActionResult Save([FromBody] SecurityModuleAdminModel model)
        {
            var required = model.SecurityModuleId == 0 ? PermissionAction.Create : PermissionAction.Edit;
            if (!_permissions.HasPermission(User, ModuleUrl, required))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso para realizar esta acción." });

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
        [RequiresModulePermission(PermissionAction.Delete)]
        public IActionResult Delete([FromBody] int securityModuleId)
        {
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
    }
}
