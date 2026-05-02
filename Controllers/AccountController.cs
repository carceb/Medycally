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

        public AccountController(ISecurityUser securityUser, IWebHostEnvironment env)
        {
            _securityUser = securityUser;
            _env          = env;
        }

        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

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
