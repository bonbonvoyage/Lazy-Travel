using Microsoft.AspNetCore.Mvc;

namespace LazyTravel.Areas.Admin.Controllers
{
    // 之後 Cookie 認證與 Role 授權建好後,改成:
    // [Authorize(Roles = "Admin,SuperAdmin")]
    [Area("Admin")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "總覽";
            return View();
        }
    }
}
