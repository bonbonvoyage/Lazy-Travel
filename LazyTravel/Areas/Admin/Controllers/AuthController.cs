using LazyTravel.Models.DTOs;
using LazyTravel.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LazyTravel.Areas.Admin.Controllers
{
	[Area("Admin")]
	public class AuthController : Controller
	{
		private readonly IEmployeeService _employeeService;

		public AuthController(IEmployeeService employeeService)
		{
			_employeeService = employeeService;
		}

		// 顯示登入畫面 (如果已經登入，直接導向後台首頁)
		[AllowAnonymous] // 允許任何人訪問此頁面
		public IActionResult Login()
		{
			if (User.Identity != null && User.Identity.IsAuthenticated)
			{
				// 🌟 修正一：導向到 Dashboard 的 Index
				return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
			}
			return View();
		}

		// 處理登入請求
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(AdminLoginDto dto)
		{
			if (!ModelState.IsValid)
			{
				return View(dto);
			}

			// 取得真實使用者的 IP
			string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

			// 呼叫 Service 驗證
			var result = _employeeService.Login(dto.Email, dto.Password, ipAddress);

			if (!result.Success)
			{
				// 為了資安，不論是帳號錯還是密碼錯，都給一樣的錯誤訊息
				ModelState.AddModelError(string.Empty, result.Message);
				return View(dto);
			}

			// ========================================================
			// 🌟 核心資安：核發 Cookie 員工識別證 (ClaimsIdentity)
			// ========================================================
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, result.EmployeeData.EmployeeID.ToString()), // 員工 ID
                new Claim(ClaimTypes.Name, result.EmployeeData.Name),                            // 姓名
                new Claim(ClaimTypes.Email, result.EmployeeData.Email)                           // Email
            };

			// 將員工擁有的所有「權限代碼 (PermissionCode)」塞入識別證
			foreach (var perm in result.EmployeeData.Permissions)
			{
				claims.Add(new Claim("Permission", perm));
			}

			// 建立身分識別，指定 AuthenticationScheme 為 "AdminAuth"
			var claimsIdentity = new ClaimsIdentity(claims, "AdminAuth");

			// 設定 Cookie 屬性
			var authProperties = new AuthenticationProperties
			{
				IsPersistent = dto.RememberMe // 是否記住我
			};

			// 登入：將加密後的 Cookie 寫入使用者瀏覽器
			await HttpContext.SignInAsync(
				"AdminAuth",
				new ClaimsPrincipal(claimsIdentity),
				authProperties);

			// 🌟 修正二：登入成功，導向到 Dashboard 的 Index
			return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
		}

		// 登出
		[HttpPost]
		public async Task<IActionResult> Logout()
		{
			// 清除名為 "AdminAuth" 的 Cookie
			await HttpContext.SignOutAsync("AdminAuth");
			return RedirectToAction("Login", "Auth", new { area = "Admin" });
		}

		// 權限不足的導向頁面
		[AllowAnonymous]
		public IActionResult AccessDenied()
		{
			return View();
		}
	}
}