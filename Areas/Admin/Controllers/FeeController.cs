using Medycally.Core;
using Medycally.Core.Security;
using Medycally.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Medycally.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class FeeController : Controller
    {
        private const string ModuleUrl = "/Admin/Fee";

        private readonly IClinicSpecialtyFee _fee;
        private readonly IExchangeRate       _exchangeRate;
        private readonly IPermissionService  _permissions;

        public FeeController(IClinicSpecialtyFee fee, IExchangeRate exchangeRate, IPermissionService permissions)
        {
            _fee          = fee;
            _exchangeRate = exchangeRate;
            _permissions  = permissions;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult GetByClinic(int clinicId)
        {
            if (clinicId <= 0) return BadRequest(new { message = "ClinicId inválido." });
            var fees = _fee.GetByClinic(clinicId);
            return Json(fees);
        }

        [HttpPost]
        public IActionResult Save([FromBody] ClinicSpecialtyFeeModel model)
        {
            if (model.ClinicId <= 0 || model.SpecialtyId <= 0)
                return BadRequest(new { message = "Datos inválidos." });
            if (model.FeeUSD <= 0)
                return BadRequest(new { message = "La tarifa en USD debe ser mayor a cero." });

            var required = model.ClinicSpecialtyFeeId == 0 ? PermissionAction.Create : PermissionAction.Edit;
            if (!_permissions.HasPermission(User, ModuleUrl, required))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso para realizar esta acción." });

            var rates = _exchangeRate.GetAll();
            var usd   = rates.FirstOrDefault(r => r.CurrencyCode == "USD");
            if (usd == null)
                return BadRequest(new { message = "No hay tasa de cambio disponible. Espere la sincronización con el BCV." });

            model.ExchangeRateUsed = usd.Rate;
            model.FeeVES           = Math.Round(model.FeeUSD * usd.Rate, 2);

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var id     = _fee.Save(model, userId);

            return Ok(new
            {
                clinicSpecialtyFeeId = id,
                feeUSD               = model.FeeUSD,
                feeVES               = model.FeeVES,
                exchangeRateUsed     = model.ExchangeRateUsed,
                updatedAt            = DateTime.Now
            });
        }
    }
}
