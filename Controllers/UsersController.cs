using Microsoft.AspNetCore.Mvc;

namespace Medycally.Controllers
{
    public class UsersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
