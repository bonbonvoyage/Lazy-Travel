using Microsoft.AspNetCore.Mvc;

namespace LazyTravel.Controllers
{
    public class HomeController : Controller
    {
        // 🌟 假首頁(Views/Home/Index.cshtml)已移除,首頁改由前端 Vue 專案負責。
        // Error() 保留:Program.cs 的 app.UseExceptionHandler("/Home/Error") 還會用到這個路由。
        public IActionResult Error()
        {
            return View();
        }
    }
}
