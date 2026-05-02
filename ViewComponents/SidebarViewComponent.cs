using Medycally.Core;
using Medycally.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Medycally.ViewComponents
{
    public class SidebarViewComponent : ViewComponent
    {
        private readonly ISecurityModule _securityModule;

        public SidebarViewComponent(ISecurityModule securityModule)
        {
            _securityModule = securityModule;
        }

        public IViewComponentResult Invoke()
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out int securityUserId))
                return View(new List<NavigationModuleModel>());

            var modules = _securityModule.GetUserPermissions(securityUserId);
            ViewData["CurrentPath"] = HttpContext.Request.Path.Value ?? "";
            return View(modules);
        }
    }
}
