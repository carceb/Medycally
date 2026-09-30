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
    public class DoctorController : Controller
    {
        private const string ModuleUrl = "/Admin/Doctor";

        private readonly IDoctor                  _doctor;
        private readonly ISpecialty               _specialty;
        private readonly ICommonData              _commonData;
        private readonly IGeography               _geography;
        private readonly IClinic                  _clinic;
        private readonly IPricingStructure        _pricing;
        private readonly IDoctorPricingStructure  _doctorPricing;
        private readonly IPermissionService       _permissions;

        public DoctorController(IDoctor doctor, ISpecialty specialty, ICommonData commonData,
            IGeography geography, IClinic clinic, IPricingStructure pricing,
            IDoctorPricingStructure doctorPricing, IPermissionService permissions)
        {
            _doctor        = doctor;
            _specialty     = specialty;
            _commonData    = commonData;
            _geography     = geography;
            _clinic        = clinic;
            _pricing       = pricing;
            _doctorPricing = doctorPricing;
            _permissions   = permissions;
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

            var required = model.DoctorId == 0 ? PermissionAction.Create : PermissionAction.Edit;
            if (!_permissions.HasPermission(User, ModuleUrl, required))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso para realizar esta acción." });

            var id = _doctor.AddOrEdit(model);
            return Ok(new { doctorId = id });
        }

        [HttpPost]
        [RequiresModulePermission(PermissionAction.Delete)]
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
        [RequiresModulePermission(PermissionAction.Edit)]
        public IActionResult SaveSpecialties([FromBody] SaveSpecialtiesRequest request)
        {
            _doctor.SaveSpecialties(request.DoctorId, request.SpecialtyIds ?? []);
            return Ok();
        }

        [HttpGet]
        public IActionResult GetDoctorPricing(int doctorId)
        {
            try
            {
                var list = _doctorPricing.GetByDoctor(doctorId);
                return Json(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetAllPricing()
        {
            try
            {
                int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int securityUserId);
                bool isSuperAdmin = string.Equals(User.FindFirst("IsSuperAdmin")?.Value, "true",
                                                  StringComparison.OrdinalIgnoreCase);
                int? doctorId = int.TryParse(User.FindFirst("DoctorId")?.Value, out int did) && did > 0
                                ? did : null;
                bool hasGlobalScope = string.Equals(User.FindFirst("HasGlobalScope")?.Value, "true",
                                                    StringComparison.OrdinalIgnoreCase);

                var userClinicIds = _clinic.GetByUser(securityUserId, isSuperAdmin, doctorId, hasGlobalScope)
                                           .Select(c => c.ClinicId)
                                           .ToHashSet();

                var list = _pricing.GetAll()
                                   .Where(p => userClinicIds.Contains(p.ClinicId))
                                   .ToList();
                return Json(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult SaveDoctorPricing([FromBody] DoctorPricingStructureModel model)
        {
            try
            {
                if (model.DoctorId <= 0)
                    return BadRequest(new { message = "Médico inválido." });
                if (model.PricingStructureId <= 0)
                    return BadRequest(new { message = "Debe seleccionar una estructura de precio." });
                if (model.StatusId != 1 && model.StatusId != 2)
                    return BadRequest(new { message = "Estatus inválido." });

                var required = model.DoctorPricingStructureId == 0 ? PermissionAction.Create : PermissionAction.Edit;
                if (!_permissions.HasPermission(User, ModuleUrl, required))
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso para realizar esta acción." });

                var id = _doctorPricing.AddOrEdit(model);
                return Ok(new { doctorPricingStructureId = id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequiresModulePermission(PermissionAction.Delete)]
        public IActionResult DeleteDoctorPricing([FromBody] int doctorPricingStructureId)
        {
            try
            {
                _doctorPricing.Delete(doctorPricingStructureId);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }

    public class SaveSpecialtiesRequest
    {
        public int DoctorId { get; set; }
        public List<int>? SpecialtyIds { get; set; }
    }
}
