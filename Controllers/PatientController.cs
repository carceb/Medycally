using Medycally.Core;
using Medycally.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medycally.Controllers
{
    [Authorize]
    public class PatientController : Controller
    {
        private readonly IPatient    _patient;
        private readonly IGeography  _geography;
        private readonly ICommonData _commonData;

        public PatientController(IPatient patient, IGeography geography, ICommonData commonData)
        {
            _patient    = patient;
            _geography  = geography;
            _commonData = commonData;
        }

        public IActionResult Index()
        {
            var patients = _patient.GetAll(null);
            ViewBag.Sexes          = _commonData.GetAll();
            ViewBag.Relationships  = _commonData.GetAllRelationship();
            ViewBag.States         = _geography.GetAllStates();
            ViewBag.Municipalities = _geography.GetAllMunicipalities();
            return View(patients);
        }

        [HttpGet]
        public IActionResult GetFamily(int patientId)
        {
            try
            {
                var family = _patient.GetFamily(patientId);
                return Json(family);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetByCedula(int cedula)
        {
            try
            {
                var p = _patient.GetByIdNumber(cedula);
                if (p == null) return NotFound(new { message = "Paciente no encontrado." });
                return Json(p);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult Save([FromBody] PatientModel model)
        {
            if (string.IsNullOrWhiteSpace(model.PatientName))
                return BadRequest(new { message = "El nombre del paciente es requerido." });

            var id = _patient.AddOrEdit(model);
            return Ok(new { patientId = id });
        }

        [HttpPost]
        public IActionResult Delete([FromBody] int patientId)
        {
            try
            {
                _patient.Delete(patientId);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult SaveGuardian([FromBody] SaveGuardianRequest request)
        {
            try
            {
                _patient.LinkGuardian(request.PatientId, request.GuardianPatientId, request.RelationshipId);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult RemoveGuardian([FromBody] RemoveGuardianRequest request)
        {
            try
            {
                _patient.RemoveGuardianLink(request.PatientId, request.GuardianPatientId);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }

    public class SaveGuardianRequest
    {
        public int PatientId         { get; set; }
        public int GuardianPatientId { get; set; }
        public int RelationshipId    { get; set; }
    }

    public class RemoveGuardianRequest
    {
        public int PatientId         { get; set; }
        public int GuardianPatientId { get; set; }
    }
}
