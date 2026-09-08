using Microsoft.AspNetCore.Mvc;

namespace LazyTravel.Controllers
{
    public class MembersController : Controller
    {
        // GET /Members  或  /Members/Profile
        public IActionResult Index() => Profile();

        public IActionResult Profile()
        {
            ViewData["Title"] = "個人頁面";
            return View("Profile");
        }
    }
}
