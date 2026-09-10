using Microsoft.AspNetCore.Mvc;
namespace LazyTravel.Controllers;
// Optional profile details are edited in the existing personal center.
public class AccountController : Controller
{
    [HttpGet] public IActionResult Register() => View();
    [HttpGet] public IActionResult Setup() => RedirectToAction("Profile", "Members");
}
