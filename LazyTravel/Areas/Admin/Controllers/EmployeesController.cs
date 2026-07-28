using LazyTravel.Models.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace LazyTravel.Areas.Admin.Controllers
{
	[Area("Admin")]
	public class EmployeesController : Controller
	{
		private readonly IEmployeeService _employeeService;

		public EmployeesController(IEmployeeService employeeService)
		{
			_employeeService = employeeService;
		}

		// ==========================================
		// 頁面進入點 (雙頁籤)
		// ==========================================
		public IActionResult Index(string keyword, string subTab = "list", int page = 1)
		{
			ViewBag.SubTab = subTab;
			ViewBag.Keyword = keyword;

			if (subTab == "list")
			{
				var employees = _employeeService.GetAllEmployees(keyword);
				ViewBag.Roles = _employeeService.GetAllRoles(); // 供彈窗選角色用
				return View(employees);
			}
			else if (subTab == "logs")
			{
				var logsResult = _employeeService.GetEmployeeAdminLogs(keyword, page);
				ViewBag.EmployeeLogs = logsResult.Data;
				ViewBag.CurrentPage = page;
				ViewBag.TotalPages = (int)System.Math.Ceiling(logsResult.TotalCount / 10.0);
				ViewBag.TotalCount = logsResult.TotalCount;
				return View();
			}

			return View();
		}

		// ==========================================
		// 員工管理 POST
		// ==========================================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Promote(string name, string email, string password, List<int> roleIds)
		{
			if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || roleIds == null || roleIds.Count == 0)
			{
				TempData["ErrorMessage"] = "建立失敗：請填寫姓名、Email、密碼並至少勾選一項職務角色！";
				return RedirectToAction(nameof(Index), new { subTab = "list" });
			}

			int currentAdminId = 1;
			var result = _employeeService.CreateEmployee(name, email, password, roleIds, currentAdminId);

			SetTempDataMessage(result.Success, result.Message);
			return RedirectToAction(nameof(Index), new { subTab = "list" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult EditEmployeeRoles(int employeeId, List<int> roleIds)
		{
			if (roleIds == null || roleIds.Count == 0)
			{
				TempData["ErrorMessage"] = "修改失敗：員工至少必須保留一項職務角色！";
				return RedirectToAction(nameof(Index), new { subTab = "list" });
			}

			int currentAdminId = 1;
			var result = _employeeService.EditEmployeeRoles(employeeId, roleIds, currentAdminId);

			SetTempDataMessage(result.Success, result.Message);
			return RedirectToAction(nameof(Index), new { subTab = "list" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Demote(int adminId)
		{
			int currentAdminId = 1;
			var result = _employeeService.DemoteEmployee(adminId, currentAdminId);

			SetTempDataMessage(result.Success, result.Message);
			return RedirectToAction(nameof(Index), new { subTab = "list" });
		}

		private void SetTempDataMessage(bool success, string message)
		{
			if (success) TempData["SuccessMessage"] = message;
			else TempData["ErrorMessage"] = message;
		}
	}
}