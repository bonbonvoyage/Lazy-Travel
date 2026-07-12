using Microsoft.AspNetCore.Mvc;

namespace LazyTravel.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "首頁";
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
