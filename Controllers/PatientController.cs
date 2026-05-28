using Medycally.Core;
using Medycally.Core.Security;
using Medycally.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Medycally.Controllers
{
    [Authorize]
    public class PatientController : Controller
    {
        private const string ModuleUrl = "/Patient/Index";

        private readonly IPatient           _patient;
        private readonly IGeography         _geography;
        private readonly ICommonData        _commonData;
        private readonly IPatientHistory    _patientHistory;
        private readonly IMedicalAttention  _medical;
        private readonly IPermissionService _permissions;

        public PatientController(IPatient patient, IGeography geography, ICommonData commonData,
            IPatientHistory patientHistory, IMedicalAttention medical, IPermissionService permissions)
        {
            _patient        = patient;
            _geography      = geography;
            _commonData     = commonData;
            _patientHistory = patientHistory;
            _medical        = medical;
            _permissions    = permissions;
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

            var required = model.PatientId == 0 ? PermissionAction.Create : PermissionAction.Edit;
            if (!_permissions.HasPermission(User, ModuleUrl, required))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso para realizar esta acción." });

            var id = _patient.AddOrEdit(model);
            return Ok(new { patientId = id });
        }

        [HttpPost]
        [RequiresModulePermission(PermissionAction.Delete)]
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
        [RequiresModulePermission(PermissionAction.Edit)]
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

        [HttpGet]
        public IActionResult History(int id)
        {
            var patientId = id;
            var patient = _patient.GetById(patientId);
            if (patient == null) return NotFound();

            var history = _patientHistory.GetByPatientId(patientId);

            List<MedicalAttentionModel> consultations;
            if (patient.PatientIdNumber.HasValue && patient.PatientIdNumber.Value > 0)
                consultations = _medical.GetHistoryByPatient(patient.PatientIdNumber.Value);
            else if (patient.HasGuardian)
            {
                var family = _patient.GetFamily(patientId);
                var guardian = family.FirstOrDefault(f => f.Role == "guardian");
                consultations = guardian?.PatientIdNumber.HasValue == true && guardian.PatientIdNumber.Value > 0
                    ? _medical.GetHistoryByGuardian(guardian.PatientIdNumber.Value)
                    : [];
            }
            else
                consultations = [];

            ViewBag.Patient       = patient;
            ViewBag.Consultations = consultations;
            return View(history ?? new PatientHistoryModel { PatientId = patientId });
        }

        [HttpPost]
        [RequiresModulePermission(PermissionAction.Edit)]
        public IActionResult SaveHistory([FromBody] PatientHistoryModel model)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                _patientHistory.Save(model, userId);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequiresModulePermission(PermissionAction.Edit)]
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
