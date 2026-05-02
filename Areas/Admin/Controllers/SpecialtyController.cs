using Medycally.Core;
using Medycally.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medycally.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize]
	public class SpecialtyController : Controller
	{
		private readonly ISpecialty _specialty;

		public SpecialtyController(ISpecialty specialty)
		{
			_specialty = specialty;
		}

		public IActionResult Index()
		{
			var specialties = _specialty.GetAll();
			return View(specialties);
		}

		[HttpPost]
		public IActionResult Save([FromBody] SpecialtyModel model)
		{
			if (string.IsNullOrWhiteSpace(model.SpecialtyName))
				return BadRequest(new { message = "El nombre de la especialidad es requerido." });

			var id = _specialty.AddOrEdit(model);
			return Ok(new { specialtyId = id });
		}

		[HttpPost]
		public IActionResult Delete([FromBody] int specialtyId)
		{
			_specialty.Delete(specialtyId);
			return Ok();
		}
	}
}
