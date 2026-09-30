using System.Security.Claims;
using Medycally.Core;
using Medycally.Core.Security;
using Medycally.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medycally.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize]
	public class ClinicController : Controller
	{
		private const string ModuleUrl = "/Admin/Clinic";

		private readonly IClinic            _clinic;
		private readonly IClinicType        _clinicType;
		private readonly IGeography         _geography;
		private readonly IDoctorSchedule    _schedule;
		private readonly IPricingStructure  _pricing;
		private readonly IPermissionService _permissions;

		public ClinicController(IClinic clinic, IClinicType clinicType, IGeography geography,
			IDoctorSchedule schedule, IPricingStructure pricing, IPermissionService permissions)
		{
			_clinic      = clinic;
			_clinicType  = clinicType;
			_geography   = geography;
			_schedule    = schedule;
			_pricing     = pricing;
			_permissions = permissions;
		}

		public IActionResult Index()
		{
			int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int securityUserId);
			bool isSuperAdmin = string.Equals(User.FindFirst("IsSuperAdmin")?.Value, "true", StringComparison.OrdinalIgnoreCase);
			int? doctorId = int.TryParse(User.FindFirst("DoctorId")?.Value, out int did) ? did : null;
			bool hasGlobalScope = string.Equals(User.FindFirst("HasGlobalScope")?.Value, "true", StringComparison.OrdinalIgnoreCase);

			var clinics = _clinic.GetByUser(securityUserId, isSuperAdmin, doctorId, hasGlobalScope);
			ViewBag.ClinicTypes    = _clinicType.GetAll();
			ViewBag.States         = _geography.GetAllStates();
			ViewBag.Municipalities = _geography.GetAllMunicipalities();
			return View(clinics);
		}

		[HttpGet]
		public IActionResult GetMunicipalities(int stateId)
		{
			var municipalities = _geography.GetMunicipalityByStateId(stateId);
			return Json(municipalities);
		}

		[HttpPost]
		public IActionResult Save([FromBody] ClinicModel model)
		{
			if (string.IsNullOrWhiteSpace(model.ClinicName))
				return BadRequest(new { message = "El nombre de la clínica es requerido." });

			if (model.MunicipalityId == 0)
				return BadRequest(new { message = "Debe seleccionar un municipio." });

			var required = model.ClinicId == 0 ? PermissionAction.Create : PermissionAction.Edit;
			if (!_permissions.HasPermission(User, ModuleUrl, required))
				return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso para realizar esta acción." });

			var id = _clinic.AddOrEdit(model);
			return Ok(new { clinicId = id });
		}

		[HttpPost]
		[RequiresModulePermission(PermissionAction.Delete)]
		public IActionResult Delete([FromBody] int clinicId)
		{
			_clinic.Delete(clinicId);
			return Ok();
		}

		[HttpGet]
		public IActionResult GetDoctors(int clinicId)
		{
			try
			{
				var doctors = _clinic.GetDoctors(clinicId);
				return Json(doctors);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message });
			}
		}

		[HttpPost]
		[RequiresModulePermission(PermissionAction.Edit)]
		public IActionResult SaveDoctors([FromBody] SaveDoctorsRequest request)
		{
			_clinic.SaveDoctors(request.ClinicId, request.DoctorIds ?? []);
			return Ok();
		}

		[HttpGet]
		public IActionResult GetSchedule(int clinicId, int doctorId)
		{
			try
			{
				var schedules = _schedule.GetByClinicAndDoctor(clinicId, doctorId);
				return Json(schedules);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message });
			}
		}

		[HttpPost]
		public IActionResult SaveSchedule([FromBody] AdminScheduleModel model)
		{
			try
			{
				var required = model.DoctorScheduleId == 0 ? PermissionAction.Create : PermissionAction.Edit;
				if (!_permissions.HasPermission(User, ModuleUrl, required))
					return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso para realizar esta acción." });

				var id = _schedule.SaveSchedule(model);
				return Ok(new { scheduleId = id });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message });
			}
		}

		[HttpPost]
		[RequiresModulePermission(PermissionAction.Delete)]
		public IActionResult DeleteSchedule([FromBody] int doctorScheduleId)
		{
			try
			{
				_schedule.DeleteSchedule(doctorScheduleId);
				return Ok();
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message });
			}
		}

		[HttpGet]
		public IActionResult GetPricing(int clinicId)
		{
			try
			{
				var list = _pricing.GetByClinic(clinicId);
				return Json(list);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message });
			}
		}

		[HttpPost]
		public IActionResult SavePricing([FromBody] PricingStructureModel model)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(model.PricingStructureName))
					return BadRequest(new { message = "El nombre de la estructura es requerido." });
				if (model.PricingStructureValue < 0)
					return BadRequest(new { message = "El valor no puede ser negativo." });
				if (model.ClinicId <= 0)
					return BadRequest(new { message = "Clínica inválida." });

				var required = model.PricingStructureId == 0 ? PermissionAction.Create : PermissionAction.Edit;
				if (!_permissions.HasPermission(User, ModuleUrl, required))
					return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso para realizar esta acción." });

				var id = _pricing.AddOrEdit(model);
				return Ok(new { pricingStructureId = id });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message });
			}
		}

		[HttpPost]
		[RequiresModulePermission(PermissionAction.Delete)]
		public IActionResult DeletePricing([FromBody] int pricingStructureId)
		{
			try
			{
				_pricing.Delete(pricingStructureId);
				return Ok();
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message });
			}
		}
	}

	public class SaveDoctorsRequest
	{
		public int ClinicId { get; set; }
		public List<int>? DoctorIds { get; set; }
	}
}
