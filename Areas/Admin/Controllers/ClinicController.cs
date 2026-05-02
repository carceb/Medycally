using Medycally.Core;
using Medycally.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medycally.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize]
	public class ClinicController : Controller
	{
		private readonly IClinic         _clinic;
		private readonly IClinicType     _clinicType;
		private readonly IGeography      _geography;
		private readonly IDoctorSchedule _schedule;

		public ClinicController(IClinic clinic, IClinicType clinicType, IGeography geography, IDoctorSchedule schedule)
		{
			_clinic      = clinic;
			_clinicType  = clinicType;
			_geography   = geography;
			_schedule    = schedule;
		}

		public IActionResult Index()
		{
			var clinics = _clinic.GetAll();
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

			var id = _clinic.AddOrEdit(model);
			return Ok(new { clinicId = id });
		}

		[HttpPost]
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
				var id = _schedule.SaveSchedule(model);
				return Ok(new { scheduleId = id });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = ex.Message });
			}
		}

		[HttpPost]
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
	}

	public class SaveDoctorsRequest
	{
		public int ClinicId { get; set; }
		public List<int>? DoctorIds { get; set; }
	}
}
