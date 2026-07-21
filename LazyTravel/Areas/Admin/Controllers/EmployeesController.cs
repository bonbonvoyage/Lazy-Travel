using LazyTravel.Models.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace LazyTravel.Areas.Admin.Controllers
{
	[Area("Admin")]
	// 🌟 【資安門禁】只有帶有 "Mod_System" 權限的「超級管理員」能進來這裡。
	// ⚠️ 備註：因為我們還沒實作登入畫面，我先暫時把這行註解起來讓你測試畫面。
	// 等下一關我們做完 Login 登入，記得把這行的 // 拿掉！
	// [Authorize(Policy = "RequireSystemPermission")]
	public class EmployeesController : Controller
	{
		private readonly IEmployeeService _employeeService;

		public EmployeesController(IEmployeeService employeeService)
		{
			_employeeService = employeeService;
		}

		public IActionResult Index()
		{
			// 直接呼叫 Service 取得整理好的員工列表 DTO
			var employees = _employeeService.GetAllEmployees();
			return View(employees);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Promote(string email, List<string> permissions)
		{
			if (string.IsNullOrWhiteSpace(email) || permissions == null || permissions.Count == 0)
			{
				TempData["ErrorMessage"] = "指派失敗：請輸入會員 Email 並至少勾選一項權限！";
				return RedirectToAction(nameof(Index));
			}

			// TODO: 等實作登入後，改成從 Cookie Claims 抓取當前登入者的 ID
			// int currentAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
			int currentAdminId = 1; // 暫時寫死超級管理員 ID = 1 測試

			var result = _employeeService.PromoteToAdmin(email, permissions, currentAdminId);

			if (result.Success) TempData["SuccessMessage"] = result.Message;
			else TempData["ErrorMessage"] = result.Message;

			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult EditPermissions(int adminId, List<string> permissions)
		{
			if (permissions == null || permissions.Count == 0)
			{
				TempData["ErrorMessage"] = "修改失敗：至少必須保留一項權限！";
				return RedirectToAction(nameof(Index));
			}

			int currentAdminId = 1;

			var result = _employeeService.EditPermissions(adminId, permissions, currentAdminId);

			if (result.Success) TempData["SuccessMessage"] = result.Message;
			else TempData["ErrorMessage"] = result.Message;

			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Demote(int adminId)
		{
			int currentAdminId = 1;

			var result = _employeeService.DemoteAdmin(adminId, currentAdminId);

			if (result.Success) TempData["SuccessMessage"] = result.Message;
			else TempData["ErrorMessage"] = result.Message;

			return RedirectToAction(nameof(Index));
		}
	}
}