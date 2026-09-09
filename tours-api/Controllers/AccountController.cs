using Microsoft.AspNetCore.Mvc;
namespace LazyTravel.Controllers;
// UI-only onboarding: no account or member data is written by these routes.
public class AccountController : Controller
{
    [HttpGet] public IActionResult Register() => View();
    [HttpGet] public IActionResult Setup() => View();
}
