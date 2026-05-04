using Medycally.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Medycally.Controllers
{
    public class AccountController : Controller
    {
        private readonly ISecurityUser _securityUser;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _email;

        public AccountController(ISecurityUser securityUser, IWebHostEnvironment env, IEmailService email)
        {
            _securityUser = securityUser;
            _env          = env;
            _email        = email;
        }

        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewBag.Success = TempData["ActivationSuccess"] as string;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            string hash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(password))
            ).ToLower();

            var user = _securityUser.Login(email, hash);

            if (user == null)
            {
                ViewBag.Error = "Correo o contraseña incorrectos.";
                return View();
            }

            var claims = BuildClaims(user);
            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> QuickLogin()
        {
            if (!_env.IsDevelopment())
                return NotFound();

            string hash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes("Admin123"))
            ).ToLower();

            var user = _securityUser.Login("admin@medycally.com", hash);
            if (user == null)
                return RedirectToAction("Login");

            var claims    = BuildClaims(user);
            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Activate(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login");

            var user = _securityUser.GetByToken(token);
            if (user == null)
            {
                ViewBag.Error = "El enlace de activación no es válido o ha expirado. Contacta al administrador.";
                return View();
            }

            ViewBag.Token    = token;
            ViewBag.UserName = user.UserName;
            ViewBag.Email    = user.UserEmail;
            return View();
        }

        [HttpPost]
        public IActionResult Activate(string token, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                ViewBag.Token    = token;
                ViewBag.PasswordError = "La contraseña debe tener al menos 6 caracteres.";
                var u = _securityUser.GetByToken(token);
                ViewBag.UserName = u?.UserName;
                ViewBag.Email    = u?.UserEmail;
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Token    = token;
                ViewBag.PasswordError = "Las contraseñas no coinciden.";
                var u = _securityUser.GetByToken(token);
                ViewBag.UserName = u?.UserName;
                ViewBag.Email    = u?.UserEmail;
                return View();
            }

            string hash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(password))
            ).ToLower();

            bool success = _securityUser.Activate(token, hash);

            if (!success)
            {
                ViewBag.Error = "El enlace de activación no es válido o ha expirado. Contacta al administrador.";
                return View();
            }

            TempData["ActivationSuccess"] = "Cuenta activada correctamente. Ya puedes iniciar sesión.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Ingresa tu correo electrónico.";
                return View();
            }

            var user = _securityUser.ForgotPassword(email.Trim());

            if (user != null && !string.IsNullOrEmpty(user.ResetToken))
            {
                try
                {
                    var baseUrl  = $"{Request.Scheme}://{Request.Host}";
                    var resetUrl = $"{baseUrl}/Account/ResetPassword?token={user.ResetToken}";
                    await _email.SendPasswordResetEmailAsync(user.UserEmail, user.UserName, resetUrl);
                }
                catch { /* silencioso — no revelar si el email existe */ }
            }

            // Siempre mostrar el mismo mensaje por seguridad
            ViewBag.Sent = true;
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login");

            var user = _securityUser.GetByResetToken(token);
            if (user == null)
            {
                ViewBag.Error = "El enlace ha expirado o no es válido. Solicita uno nuevo.";
                return View();
            }

            ViewBag.Token    = token;
            ViewBag.UserName = user.UserName;
            ViewBag.Email    = user.UserEmail;
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string token, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                var u = _securityUser.GetByResetToken(token);
                ViewBag.Token    = token;
                ViewBag.UserName = u?.UserName;
                ViewBag.Email    = u?.UserEmail;
                ViewBag.PasswordError = "La contraseña debe tener al menos 6 caracteres.";
                return View();
            }

            if (password != confirmPassword)
            {
                var u = _securityUser.GetByResetToken(token);
                ViewBag.Token    = token;
                ViewBag.UserName = u?.UserName;
                ViewBag.Email    = u?.UserEmail;
                ViewBag.PasswordError = "Las contraseñas no coinciden.";
                return View();
            }

            string hash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(password))
            ).ToLower();

            bool success = _securityUser.ResetPassword(token, hash);

            if (!success)
            {
                ViewBag.Error = "El enlace ha expirado o no es válido. Solicita uno nuevo.";
                return View();
            }

            TempData["ActivationSuccess"] = "Contraseña restablecida correctamente. Ya puedes iniciar sesión.";
            return RedirectToAction("Login");
        }

        private static List<Claim> BuildClaims(Medycally.Models.SecurityUserModel user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.SecurityUserId.ToString()),
                new(ClaimTypes.Name,            user.UserName),
                new(ClaimTypes.Email,           user.UserEmail),
                new("SecurityRoleId",           user.SecurityRoleId.ToString()),
                new(ClaimTypes.Role,            user.RoleName),
                new("RoleLevel",                user.RoleLevel.ToString())
            };
            if (user.DoctorId.HasValue)
                claims.Add(new("DoctorId", user.DoctorId.Value.ToString()));
            return claims;
        }
    }
}
