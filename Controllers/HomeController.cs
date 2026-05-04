using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Medycally.Models;
using Medycally.Core;

namespace Medycally.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IAppointmentQuery _appointmentQuery;
    private readonly IClinic           _clinic;
    private readonly IGeography        _geography;
    private readonly ICommonData       _commonData;
    private readonly IPatient          _patient;

    public HomeController(IAppointmentQuery appointmentQuery, IClinic clinic,
                          IGeography geography, ICommonData commonData, IPatient patient)
    {
        _appointmentQuery = appointmentQuery;
        _clinic           = clinic;
        _geography        = geography;
        _commonData       = commonData;
        _patient          = patient;
    }

    [AllowAnonymous]
    public IActionResult Landing()
    {
        return View();
    }

    public IActionResult Index()
    {
        ViewBag.Statuses       = _appointmentQuery.GetStatuses();
        ViewBag.Sexes          = _commonData.GetAll();
        ViewBag.Relationships  = _commonData.GetAllRelationship();
        ViewBag.States         = _geography.GetAllStates();
        ViewBag.Municipalities = _geography.GetAllMunicipalities();
        return View();
    }

    [HttpPost]
    public IActionResult UpdateAppointmentStatus([FromBody] UpdateStatusRequest request)
    {
        try
        {
            _appointmentQuery.UpdateStatus(request.AppointmentId, request.AppointmentStatusId);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet]
    public IActionResult GetAppointments(int clinicId, DateTime? date)
    {
        try
        {
            var appointments = _appointmentQuery.GetByClinic(clinicId, date ?? DateTime.Today);
            return Json(appointments);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet]
    public IActionResult GetAppointmentDetail(int appointmentId)
    {
        try
        {
            var detail = _appointmentQuery.GetById(appointmentId);
            if (detail == null) return NotFound(new { message = "Cita no encontrada." });
            return Json(detail);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost]
    public IActionResult RegisterPatient([FromBody] RegisterPatientRequest request)
    {
        try
        {
            int guardianPatientId = 0;
            if (request.Guardian != null)
            {
                request.Guardian.IsGuardianOnly = true;
                guardianPatientId = _patient.AddOrEdit(request.Guardian);
            }

            var patientId = _patient.AddOrEdit(request.Patient);

            if (guardianPatientId > 0)
                _patient.LinkGuardian(patientId, guardianPatientId, request.RelationshipId);

            if (request.AppointmentId > 0)
                _appointmentQuery.SetPatientId(request.AppointmentId, patientId);

            return Ok(new { patientId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet]
    public IActionResult GetPatientDetail(int appointmentId)
    {
        var detail = _appointmentQuery.GetById(appointmentId);
        if (detail == null) return NotFound(new { message = "Cita no encontrada." });

        if (!detail.PatientId.HasValue)
            return NotFound(new { message = "Paciente no registrado." });

        var patient = _patient.GetById(detail.PatientId.Value);
        if (patient == null)
            return NotFound(new { message = "Paciente no encontrado." });

        var family         = _patient.GetFamily(patient.PatientId);
        var guardianMember = family.FirstOrDefault(f => f.Role == "guardian");
        string? guardianName = null, guardianPhone = null, guardianIdNumber = null, relationshipName = null;
        bool isMinor = false;
        if (guardianMember != null)
        {
            isMinor          = true;
            relationshipName = guardianMember.RelationshipName;
            var guardian     = _patient.GetById(guardianMember.PatientId);
            if (guardian != null)
            {
                guardianName     = guardian.PatientName;
                guardianPhone    = guardian.PatientMainPhone > 0 ? guardian.PatientMainPhone.ToString() : null;
                guardianIdNumber = guardian.PatientIdNumber?.ToString();
            }
        }

        return Json(new
        {
            patientId               = patient.PatientId,
            patientIdNumber         = patient.PatientIdNumber,
            patientName             = patient.PatientName,
            sexId                   = patient.SexId,
            sexName                 = patient.SexName,
            patientBirthdate        = patient.PatientBirthdate?.ToString("yyyy-MM-dd"),
            patientBirthdateDisplay = patient.PatientBirthdate?.ToString("dd/MM/yyyy"),
            age                     = patient.Age,
            patientMainPhone        = patient.PatientMainPhone,
            municipalityId          = patient.MunicipalityId,
            municipalityName        = patient.MunicipalityName,
            stateId                 = patient.StateId,
            stateName               = patient.StateName,
            patientAddress          = patient.PatientAddress,
            isMinor,
            guardianName,
            guardianPhone,
            guardianIdNumber,
            relationshipName
        });
    }

    [HttpGet]
    public IActionResult GetClinics()
    {
        try
        {
            var clinics = _clinic.GetAll();
            return Json(clinics.Select(c => new
            {
                c.ClinicId,
                ClinicName = (c.ClinicTypeName != null ? c.ClinicTypeName + " " : "") + (c.ClinicName ?? ""),
                c.StateName,
                c.MunicipalityName
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost]
    public IActionResult DeleteAppointment([FromBody] int appointmentId)
    {
        try
        {
            _appointmentQuery.Delete(appointmentId);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    public record UpdateStatusRequest(int AppointmentId, int AppointmentStatusId);

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
