using Medycally.Core;
using Medycally.Core.Security;
using Medycally.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medycally.Controllers
{
    [Authorize]
    public class MedicalController : Controller
    {
        private const string QueueModuleUrl = "/Medical/Index";

        private readonly IMedicalAttention  _medical;
        private readonly IAppointmentQuery  _appointmentQuery;
        private readonly IPatient           _patient;
        private readonly IPatientHistory    _patientHistory;
        private readonly IPermissionService _permissions;

        public MedicalController(IMedicalAttention medical, IAppointmentQuery appointmentQuery,
            IPatient patient, IPatientHistory patientHistory, IPermissionService permissions)
        {
            _medical          = medical;
            _appointmentQuery = appointmentQuery;
            _patient          = patient;
            _patientHistory   = patientHistory;
            _permissions      = permissions;
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
        [RequiresModulePermission(PermissionAction.Edit, "/Medical/Index")]
        public IActionResult StartAttention([FromBody] int appointmentId)
        {
            var detail = _appointmentQuery.GetById(appointmentId);
            if (detail == null)
                return NotFound();

            if (!detail.PatientId.HasValue || detail.PatientId.Value <= 0)
                return BadRequest(new { message = "Este paciente no está registrado en el sistema. Completa su registro antes de iniciar la atención." });

            _appointmentQuery.UpdateStatus(appointmentId, 4);
            return Ok();
        }

        [HttpGet]
        public IActionResult Patient(int id)
        {
            var detail = _appointmentQuery.GetById(id);
            if (detail == null) return NotFound();

            if (!detail.PatientId.HasValue || detail.PatientId.Value <= 0)
            {
                TempData["MedicalError"] = "Este paciente no está registrado en el sistema. Completa su registro antes de iniciar la atención.";
                return RedirectToAction("Index");
            }

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

            var required = model.AttentionId == 0 ? PermissionAction.Create : PermissionAction.Edit;
            if (!_permissions.HasPermission(User, QueueModuleUrl, required))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso para realizar esta acción." });

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

            ViewBag.Attention = existing;
            return View(detail);
        }

        [HttpGet]
        public IActionResult AttendedPatients()
        {
            var list = _medical.GetAll();
            return View(list);
        }

        [HttpGet]
        public IActionResult Calendar()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetCalendarEvents(string? start, string? end)
        {
            var startDate = DateTime.TryParse(start, out var s) ? s : DateTime.Today.AddDays(-31);
            var endDate   = DateTime.TryParse(end,   out var e) ? e : DateTime.Today.AddDays(31);

            int? doctorId = GetDoctorId();
            var list = _appointmentQuery.GetForCalendar(doctorId, startDate, endDate);

            var events = list.Select(a => new
            {
                id              = a.AppointmentId,
                title           = a.PatientName ?? "(Sin nombre)",
                start           = a.AppointmentDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                end             = a.AppointmentDate.AddMinutes(30).ToString("yyyy-MM-ddTHH:mm:ss"),
                backgroundColor = StatusBgColor(a.AppointmentStatusId),
                borderColor     = StatusColor(a.AppointmentStatusId),
                textColor       = StatusColor(a.AppointmentStatusId),
                extendedProps   = new
                {
                    appointmentId = a.AppointmentId,
                    doctorName    = a.DoctorName,
                    specialtyName = a.SpecialtyName,
                    statusId      = a.AppointmentStatusId,
                    statusName    = a.AppointmentStatusName,
                    time          = a.AppointmentTime,
                }
            });

            return Json(events);
        }

        private static string StatusColor(int statusId) => statusId switch
        {
            1 => "#6c757d",
            2 => "#198754",
            3 => "#dc3545",
            4 => "#0dcaf0",
            5 => "#6f42c1",
            _ => "#6c757d"
        };

        private static string StatusBgColor(int statusId) => statusId switch
        {
            1 => "#e2e3e5",
            2 => "#d1e7dd",
            3 => "#f8d7da",
            4 => "#cff4fc",
            5 => "#e8d5f5",
            _ => "#e2e3e5"
        };

        private int? GetDoctorId()
        {
            var val = User.FindFirst("DoctorId")?.Value;
            return int.TryParse(val, out var id) && id > 0 ? id : null;
        }
    }

}
