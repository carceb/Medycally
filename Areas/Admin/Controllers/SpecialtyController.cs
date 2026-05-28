using Medycally.Core;
using Medycally.Core.Security;
using Medycally.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medycally.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize]
	public class SpecialtyController : Controller
	{
		private const string ModuleUrl = "/Admin/Specialty";

		private readonly ISpecialty         _specialty;
		private readonly IPermissionService _permissions;

		public SpecialtyController(ISpecialty specialty, IPermissionService permissions)
		{
			_specialty   = specialty;
			_permissions = permissions;
		}

		public IActionResult Index()
		{
			var specialties = _specialty.GetAll();
			return View(specialties);
		}

		[HttpPost]
		public IActionResult Save([FromBody] SpecialtyModel model)
		{
			if (string.IsNullOrWhiteSpace(model.SpecialtyName))
				return BadRequest(new { message = "El nombre de la especialidad es requerido." });

			var required = model.SpecialtyId == 0 ? PermissionAction.Create : PermissionAction.Edit;
			if (!_permissions.HasPermission(User, ModuleUrl, required))
				return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso para realizar esta acción." });

			var id = _specialty.AddOrEdit(model);
			return Ok(new { specialtyId = id });
		}

		[HttpPost]
		[RequiresModulePermission(PermissionAction.Delete)]
		public IActionResult Delete([FromBody] int specialtyId)
		{
			_specialty.Delete(specialtyId);
			return Ok();
		}
	}
}
