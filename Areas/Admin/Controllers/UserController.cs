using Medycally.Core;
using Medycally.Core.Security;
using Medycally.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medycally.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class UserController : Controller
    {
        private const string ModuleUrl = "/Admin/User";

        private readonly IAdminUser         _adminUser;
        private readonly IDoctor            _doctor;
        private readonly IEmailService      _email;
        private readonly IConfiguration     _config;
        private readonly IPermissionService _permissions;

        public UserController(IAdminUser adminUser, IDoctor doctor, IEmailService email, IConfiguration config, IPermissionService permissions)
        {
            _adminUser   = adminUser;
            _doctor      = doctor;
            _email       = email;
            _config      = config;
            _permissions = permissions;
        }

        private string GetBaseUrl()
        {
            var configured = _config["Email:PublicBaseUrl"];
            if (!string.IsNullOrWhiteSpace(configured))
                return configured.TrimEnd('/');
            return $"{Request.Scheme}://{Request.Host}";
        }

        public IActionResult Index()
        {
            var users = _adminUser.GetAll();
            ViewBag.Roles   = _adminUser.GetAllRoles();
            ViewBag.Doctors = _doctor.GetAll();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] AdminUserModel model)
        {
            if (string.IsNullOrWhiteSpace(model.UserName))
                return BadRequest(new { message = "El nombre del usuario es requerido." });

            if (string.IsNullOrWhiteSpace(model.UserEmail))
                return BadRequest(new { message = "El correo electrónico es requerido." });

            bool isNew   = model.SecurityUserId == 0;
            var required = isNew ? PermissionAction.Create : PermissionAction.Edit;
            if (!_permissions.HasPermission(User, ModuleUrl, required))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso para realizar esta acción." });

            var saved  = _adminUser.AddOrEdit(model);

            if (isNew && !string.IsNullOrEmpty(saved.ActivationToken))
            {
                try
                {
                    var url = $"{GetBaseUrl()}/Account/Activate?token={saved.ActivationToken}";
                    await _email.SendActivationEmailAsync(saved.UserEmail!, saved.UserName!, url);
                }
                catch
                {
                    // El usuario fue creado aunque el email falle; el admin puede reenviar
                }
            }

            return Ok(new { securityUserId = saved.SecurityUserId });
        }

        [HttpPost]
        [RequiresModulePermission(PermissionAction.Delete)]
        public IActionResult Delete([FromBody] int securityUserId)
        {
            _adminUser.Delete(securityUserId);
            return Ok();
        }

        [HttpGet]
        public IActionResult GetUserClinics(int securityUserId)
        {
            try
            {
                var clinics = _adminUser.GetUserClinics(securityUserId);
                return Json(clinics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequiresModulePermission(PermissionAction.Edit)]
        public IActionResult SaveUserClinics([FromBody] SaveUserClinicsRequest request)
        {
            try
            {
                _adminUser.SaveUserClinics(request.SecurityUserId, request.ClinicIds ?? []);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        public class SaveUserClinicsRequest
        {
            public int SecurityUserId { get; set; }
            public List<int>? ClinicIds { get; set; }
        }

        [HttpPost]
        [RequiresModulePermission(PermissionAction.Edit)]
        public async Task<IActionResult> ResendActivation([FromBody] int securityUserId)
        {
            var token = _adminUser.ResendToken(securityUserId);
            if (token == null)
                return BadRequest(new { message = "No se pudo generar el enlace de activación." });

            var user = _adminUser.GetAll().FirstOrDefault(u => u.SecurityUserId == securityUserId);
            if (user == null)
                return NotFound();

            try
            {
                var url = $"{GetBaseUrl()}/Account/Activate?token={token}";
                await _email.SendActivationEmailAsync(user.UserEmail!, user.UserName!, url);
                return Ok(new { message = "Correo de activación reenviado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"No se pudo enviar el correo: {ex.Message}" });
            }
        }
    }
}
