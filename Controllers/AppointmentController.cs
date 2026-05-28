using Medycally.Core;
using Medycally.Core.Security;
using Medycally.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medycally.Controllers
{
    [AllowAnonymous]
    public class AppointmentController : Controller
    {
        private readonly ISpecialty _specialty;
        private readonly IClinic _clinic;
        private readonly IDoctorSchedule _doctorSchedule;
        private readonly IPatient _patient;
        private readonly IReason _reason;
        private readonly IGeography _geography;
        private readonly ICommonData _commonData;
        private readonly IAppointment _appointment;

        public AppointmentController(ISpecialty specialty, IClinic clinic, IDoctorSchedule doctorSchedule, IPatient patient, IReason reason, IGeography geography, ICommonData commonData, IAppointment appointment)
        {
            _specialty      = specialty;
            _clinic         = clinic;
            _doctorSchedule = doctorSchedule;
            _patient        = patient;
            _reason         = reason;
            _geography      = geography;
            _commonData     = commonData;
            _appointment    = appointment;
        }

        public IActionResult Wizard()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Save([FromBody] AppointmentModel model)
        {
            try
            {
                var id = _appointment.AddOrEdit(model);
                return Json(new { success = true, appointmentId = id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetAllRelationship()
        {
            var relationships = _commonData.GetAllRelationship();
            return Json(relationships);
        }

        [HttpGet]
        public IActionResult GetAllSex()
        {
            var sex = _commonData.GetAll();
            return Json(sex);
        }

        [HttpGet]
        public IActionResult GetAllStates()
        {
            var states = _geography.GetAllStates();
            return Json(states);
        }

        [HttpGet]
        public IActionResult GetReasons(int specialtyId)
        {
            var reasons = _reason.GetAll(specialtyId);
            return Json(reasons);
        }

        [HttpGet]
        public IActionResult GetActiveSpecialties()
        {
            var specialties = _specialty.GetActives();
            return Json(specialties);
        }

        [HttpGet]
        public IActionResult GetClinicsBySpecialty(int specialtyId)
        {
            var clinics = _clinic.GetBySpecialtyId(specialtyId);
            return Json(clinics);
        }

        [HttpGet]
        public IActionResult GetScheduleByClinicAndSpecialty(int clinicId, int specialtyId)
        {
            var schedules = _doctorSchedule.GetByClinicIdAndSpecialtyId(clinicId, specialtyId);
            return Json(schedules);
        }

        [HttpGet]
        public IActionResult GetMinorsByGuardian(int guardianPatientId)
        {
            try
            {
                var family = _patient.GetFamily(guardianPatientId);
                var dependents = family
                    .Where(f => f.Role == "dependent")
                    .Select(f => new {
                        f.PatientId,
                        f.PatientIdNumber,
                        f.PatientName,
                        f.SexName,
                        f.RelationshipName
                    }).ToList();
                return Json(dependents);
            }
            catch
            {
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public IActionResult GetPatientByIdNumber(int idNumber, string patientType)
        {
            try
            {
                // Para menores: busca al representante por su cédula y devuelve sus datos
                // para pre-llenar la sección del representante en el wizard.
                // Para adultos: busca al paciente directamente.
                if (patientType == "minor")
                {
                    var guardian = _patient.GetByIdNumber(idNumber);
                    if (guardian == null) return Json(new { found = false });
                    return Json(new
                    {
                        found                  = true,
                        patientId              = 0,
                        patientName            = "",
                        patientIdNumber        = 0,
                        age                    = 0,
                        sexId                  = 0,
                        sexText                = "—",
                        patientEmail           = "",
                        guardianPatientId      = guardian.PatientId,
                        childGuardianIdNumber  = guardian.PatientIdNumber ?? 0,
                        childGuardianName      = guardian.PatientName     ?? "—",
                        childGuardianEmail     = guardian.PatientEmail    ?? "",
                        childGuardianSexId     = guardian.SexId,
                        childGuardianBirthDate = guardian.PatientBirthdate.HasValue
                                                 ? guardian.PatientBirthdate.Value.ToString("yyyy-MM-dd")
                                                 : (string?)null,
                        childGuardianAddress   = guardian.PatientAddress  ?? "",
                        childGuardianPhone     = guardian.PatientMainPhone > 0
                                                 ? guardian.PatientMainPhone.ToString()
                                                 : "",
                        childGuardianStateId   = guardian.StateId,
                        relationshipId         = 0,
                        relationshipName       = "—"
                    });
                }
                else
                {
                    var patient = _patient.GetByIdNumber(idNumber);
                    if (patient == null) return Json(new { found = false });

                    var today = DateTime.Today;
                    var birth = patient.PatientBirthdate ?? today;
                    var age   = today.Year - birth.Year;
                    if (birth.Date > today.AddYears(-age)) age--;

                    return Json(new
                    {
                        found                 = true,
                        patientId             = patient.PatientId,
                        patientName           = patient.PatientName,
                        patientIdNumber       = patient.PatientIdNumber ?? 0,
                        age,
                        sexId                 = patient.SexId,
                        sexText               = patient.SexName        ?? "—",
                        patientEmail          = patient.PatientEmail   ?? "",
                        childGuardianIdNumber = 0,
                        childGuardianName     = "—",
                        childGuardianEmail    = "",
                        relationshipId        = 0,
                        relationshipName      = "—"
                    });
                }
            }
            catch
            {
                return Json(new { found = false, error = true });
            }
        }
    }
}
