using Medycally.Core;
using Medycally.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medycally.Controllers
{
    public class ExchangeRateController : Controller
    {
        private readonly IExchangeRate      _exchangeRate;
        private readonly BcvScraperService  _scraper;

        public ExchangeRateController(IExchangeRate exchangeRate, BcvScraperService scraper)
        {
            _exchangeRate = exchangeRate;
            _scraper      = scraper;
        }

        [HttpGet]
        public IActionResult GetRates()
        {
            var rates = _exchangeRate.GetAll();
            return Json(rates);
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> Refresh()
        {
            await _scraper.FetchNowAsync();
            var rates = _exchangeRate.GetAll();
            return Json(rates);
        }
    }
}
