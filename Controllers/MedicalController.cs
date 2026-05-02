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

        public MedicalController(IMedicalAttention medical, IAppointmentQuery appointmentQuery, IPatient patient)
        {
            _medical          = medical;
            _appointmentQuery = appointmentQuery;
            _patient          = patient;
        }

        public IActionResult Index() => View();

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
            if (detail == null || detail.PatientId == null) return NotFound();

            var patient = _patient.GetById(detail.PatientId.Value);
            if (patient != null)
            {
                detail.PatientName      = patient.PatientName      ?? detail.PatientName;
                detail.PatientAge       = patient.Age;
                detail.PatientSexName   = patient.SexName          ?? detail.PatientSexName;
                detail.PatientPhone     = patient.PatientMainPhone > 0
                    ? patient.PatientMainPhone.ToString() : detail.PatientPhone;
                detail.PatientAddress   = patient.PatientAddress   ?? detail.PatientAddress;
                detail.PatientBirthDate = patient.PatientBirthdate ?? detail.PatientBirthDate;

                // Guardian desde la familia del paciente (para menores)
                var family = _patient.GetFamily(patient.PatientId);
                var guardianMember = family.FirstOrDefault(f => f.Role == "guardian");
                if (guardianMember != null)
                {
                    var guardian = _patient.GetById(guardianMember.PatientId);
                    if (guardian != null)
                    {
                        detail.ChildGuardianName  = guardian.PatientName ?? detail.ChildGuardianName;
                        detail.ChildGuardianPhone = guardian.PatientMainPhone > 0
                            ? guardian.PatientMainPhone.ToString() : detail.ChildGuardianPhone;
                        if (guardian.PatientIdNumber.HasValue && guardian.PatientIdNumber.Value > 0)
                            detail.ChildGuardianIdNumber = guardian.PatientIdNumber.Value;
                    }
                    detail.RelationshipName = guardianMember.RelationshipName ?? detail.RelationshipName;
                }
            }

            List<MedicalAttentionModel> history;
            if (patient?.PatientIdNumber.HasValue == true && patient.PatientIdNumber.Value > 0)
                history = _medical.GetHistoryByPatient(patient.PatientIdNumber.Value);
            else if (detail.ChildGuardianIdNumber > 0)
                history = _medical.GetHistoryByGuardian(detail.ChildGuardianIdNumber);
            else
                history = new List<MedicalAttentionModel>();

            ViewBag.History  = history;
            ViewBag.Existing = _medical.GetByAppointment(id);
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

        private int? GetDoctorId()
        {
            var val = User.FindFirst("DoctorId")?.Value;
            return int.TryParse(val, out var id) && id > 0 ? id : null;
        }
    }
}
