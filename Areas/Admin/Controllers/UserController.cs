using Medycally.Core;
using Medycally.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medycally.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class UserController : Controller
    {
        private readonly IAdminUser _adminUser;
        private readonly IDoctor    _doctor;

        public UserController(IAdminUser adminUser, IDoctor doctor)
        {
            _adminUser = adminUser;
            _doctor    = doctor;
        }

        public IActionResult Index()
        {
            var users = _adminUser.GetAll();
            ViewBag.Roles   = _adminUser.GetAllRoles();
            ViewBag.Doctors = _doctor.GetAll();
            return View(users);
        }

        [HttpPost]
        public IActionResult Save([FromBody] AdminUserModel model)
        {
            if (string.IsNullOrWhiteSpace(model.UserName))
                return BadRequest(new { message = "El nombre del usuario es requerido." });

            if (string.IsNullOrWhiteSpace(model.UserEmail))
                return BadRequest(new { message = "El correo electrónico es requerido." });

            if (model.SecurityUserId == 0 && string.IsNullOrWhiteSpace(model.UserPassword))
                return BadRequest(new { message = "La contraseña es requerida para nuevos usuarios." });

            var id = _adminUser.AddOrEdit(model);
            return Ok(new { securityUserId = id });
        }

        [HttpPost]
        public IActionResult Delete([FromBody] int securityUserId)
        {
            _adminUser.Delete(securityUserId);
            return Ok();
        }
    }
}
