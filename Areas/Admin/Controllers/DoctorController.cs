using Medycally.Core;
using Medycally.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medycally.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class DoctorController : Controller
    {
        private readonly IDoctor      _doctor;
        private readonly ISpecialty   _specialty;
        private readonly ICommonData  _commonData;
        private readonly IGeography   _geography;

        public DoctorController(IDoctor doctor, ISpecialty specialty, ICommonData commonData, IGeography geography)
        {
            _doctor     = doctor;
            _specialty  = specialty;
            _commonData = commonData;
            _geography  = geography;
        }

        public IActionResult Index()
        {
            var doctors = _doctor.GetAll();
            ViewBag.Sexes    = _commonData.GetAll();
            ViewBag.States   = _geography.GetAllStates();
            ViewBag.Statuses = _commonData.GetAllStatuses();
            return View(doctors);
        }

        [HttpPost]
        public IActionResult Save([FromBody] DoctorModel model)
        {
            if (string.IsNullOrWhiteSpace(model.DoctorName))
                return BadRequest(new { message = "El nombre del médico es requerido." });

            var id = _doctor.AddOrEdit(model);
            return Ok(new { doctorId = id });
        }

        [HttpPost]
        public IActionResult Delete([FromBody] int doctorId)
        {
            _doctor.Delete(doctorId);
            return Ok();
        }

        [HttpGet]
        public IActionResult GetSpecialties(int doctorId)
        {
            try
            {
                var specialties = _doctor.GetSpecialties(doctorId);
                return Json(specialties);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult SaveSpecialties([FromBody] SaveSpecialtiesRequest request)
        {
            _doctor.SaveSpecialties(request.DoctorId, request.SpecialtyIds ?? []);
            return Ok();
        }
    }

    public class SaveSpecialtiesRequest
    {
        public int DoctorId { get; set; }
        public List<int>? SpecialtyIds { get; set; }
    }
}
