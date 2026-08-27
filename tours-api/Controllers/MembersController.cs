using Microsoft.AspNetCore.Mvc;

namespace LazyTravel.Controllers;

public class MembersController : Controller
{
	public IActionResult Profile()
	{
		return View();
	}
}