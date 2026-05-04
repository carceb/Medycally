using Medycally.Core;
using Medycally.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medycally.Controllers
{
    [Authorize]
    public class MedicalController : Controller
    {
        private readonly IMedicalAttention _medical;
        private readonly IAppointmentQuery _appointmentQuery;
        private readonly IPatient          _patient;
        private readonly IPatientHistory   _patientHistory;

        public MedicalController(IMedicalAttention medical, IAppointmentQuery appointmentQuery,
            IPatient patient, IPatientHistory patientHistory)
        {
            _medical          = medical;
            _appointmentQuery = appointmentQuery;
            _patient          = patient;
            _patientHistory   = patientHistory;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetQueue(int clinicId, string? date = null)
        {
            DateTime? parsedDate = date != null && DateTime.TryParse(date, out var d) ? d : null;
            int? doctorId = GetDoctorId();
            return Json(_medical.GetQueue(clinicId, doctorId, parsedDate));
        }

        [HttpPost]
        public IActionResult StartAttention([FromBody] int appointmentId)
        {
            _appointmentQuery.UpdateStatus(appointmentId, 4);
            return Ok();
        }

        [HttpGet]
        public IActionResult Patient(int id)
        {
            var detail = _appointmentQuery.GetById(id);
            if (detail == null) return NotFound();

            var patient = detail.PatientId.HasValue ? _patient.GetById(detail.PatientId.Value) : null;
            if (patient != null)
            {
                // Datos del paciente exclusivamente desde tabla Patient
                detail.PatientName      = patient.PatientName;
                detail.PatientIdNumber  = patient.PatientIdNumber ?? 0;
                detail.PatientAge       = patient.Age;
                detail.PatientSexName   = patient.SexName;
                detail.PatientPhone     = patient.PatientMainPhone > 0
                    ? patient.PatientMainPhone.ToString() : null;
                detail.PatientAddress   = patient.PatientAddress;
                detail.PatientBirthDate = patient.PatientBirthdate;

                // Representante exclusivamente desde PatientGuardian + Patient
                var family = _patient.GetFamily(patient.PatientId);
                var guardianMember = family.FirstOrDefault(f => f.Role == "guardian");
                if (guardianMember != null)
                {
                    detail.PatientTypeId = 2; // menor — determinado por tener representante
                    var guardian = _patient.GetById(guardianMember.PatientId);
                    if (guardian != null)
                    {
                        detail.ChildGuardianName     = guardian.PatientName;
                        detail.ChildGuardianPhone    = guardian.PatientMainPhone > 0
                            ? guardian.PatientMainPhone.ToString() : null;
                        detail.ChildGuardianIdNumber = guardian.PatientIdNumber ?? 0;
                    }
                    detail.RelationshipName = guardianMember.RelationshipName;
                }
                else
                {
                    detail.PatientTypeId = 1; // adulto
                }
            }

            List<MedicalAttentionModel> history;
            if (patient?.PatientIdNumber.HasValue == true && patient.PatientIdNumber.Value > 0)
                history = _medical.GetHistoryByPatient(patient.PatientIdNumber.Value);
            else if (detail.ChildGuardianIdNumber > 0)
                history = _medical.GetHistoryByGuardian(detail.ChildGuardianIdNumber);
            else
                history = new List<MedicalAttentionModel>();

            ViewBag.History         = history;
            ViewBag.Existing        = _medical.GetByAppointment(id);
            ViewBag.PatientHistory  = patient != null ? _patientHistory.GetByPatientId(patient.PatientId) : null;
            ViewBag.PatientId       = patient?.PatientId ?? 0;
            return View(detail);
        }

        [HttpPost]
        public IActionResult SaveAttention([FromBody] MedicalAttentionModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Diagnosis))
                return BadRequest(new { message = "El diagnóstico es requerido." });

            if (string.IsNullOrWhiteSpace(model.Treatment))
                return BadRequest(new { message = "El tratamiento es requerido." });

            var id = _medical.Save(model);
            return Ok(new { attentionId = id });
        }

        [HttpGet]
        public IActionResult MedicalReport(int id)
        {
            var detail = _appointmentQuery.GetById(id);
            if (detail == null) return NotFound();

            var patient = detail.PatientId.HasValue ? _patient.GetById(detail.PatientId.Value) : null;
            if (patient != null)
            {
                detail.PatientName      = patient.PatientName;
                detail.PatientIdNumber  = patient.PatientIdNumber ?? 0;
                detail.PatientAge       = patient.Age;
                detail.PatientSexName   = patient.SexName;
                detail.PatientPhone     = patient.PatientMainPhone > 0
                    ? patient.PatientMainPhone.ToString() : null;
                detail.PatientAddress   = patient.PatientAddress;
                detail.PatientBirthDate = patient.PatientBirthdate;

                var family = _patient.GetFamily(patient.PatientId);
                var guardianMember = family.FirstOrDefault(f => f.Role == "guardian");
                if (guardianMember != null)
                {
                    detail.PatientTypeId = 2;
                    var guardian = _patient.GetById(guardianMember.PatientId);
                    if (guardian != null)
                    {
                        detail.ChildGuardianName     = guardian.PatientName;
                        detail.ChildGuardianPhone    = guardian.PatientMainPhone > 0
                            ? guardian.PatientMainPhone.ToString() : null;
                        detail.ChildGuardianIdNumber = guardian.PatientIdNumber ?? 0;
                    }
                    detail.RelationshipName = guardianMember.RelationshipName;
                }
                else
                {
                    detail.PatientTypeId = 1;
                }
            }

            var existing = _medical.GetByAppointment(id);
            if (existing == null) return RedirectToAction("Patient", new { id });

            ViewBag.Attention      = existing;
            ViewBag.PatientHistory = patient != null ? _patientHistory.GetByPatientId(patient.PatientId) : null;
            return View(detail);
        }

        [HttpGet]
        public IActionResult AttendedPatients()
        {
            var list = _medical.GetAll();
            return View(list);
        }

        private int? GetDoctorId()
        {
            var val = User.FindFirst("DoctorId")?.Value;
            return int.TryParse(val, out var id) && id > 0 ? id : null;
        }
    }

}
