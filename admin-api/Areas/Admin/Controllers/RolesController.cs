using LazyTravel.Shared.Models.DTOs;
using LazyTravel.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace LazyTravel.Areas.Admin.Controllers
{
	[Area("Admin")]
	public class RolesController : Controller
	{
		private readonly IEmployeeService _employeeService;

		public RolesController(IEmployeeService employeeService)
		{
			_employeeService = employeeService;
		}

		// ==========================================
		// 進入點：支援 roles, permissions, logs 三頁籤
		// ==========================================
		public IActionResult Index(string keyword, string activeTab = "roles", int page = 1)
		{
			ViewBag.ActiveTab = activeTab;
			ViewBag.Keyword = keyword;

			if (activeTab == "roles")
			{
				ViewBag.RolesList = _employeeService.GetAllRoles();
				ViewBag.AllPermissions = _employeeService.GetAllPermissions();
			}
			else if (activeTab == "permissions")
			{
				ViewBag.PermissionsList = _employeeService.GetAllPermissions(keyword);
			}
			else if (activeTab == "logs")
			{
				var logsResult = _employeeService.GetRoleAdminLogs(keyword, page);
				ViewBag.RoleLogs = logsResult.Data;
				ViewBag.CurrentPage = page;
				ViewBag.TotalPages = (int)System.Math.Ceiling(logsResult.TotalCount / 10.0);
				ViewBag.TotalCount = logsResult.TotalCount;
			}

			return View();
		}

		// ==========================================
		// 角色管理 POST
		// ==========================================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult CreateRole(RoleDto dto, List<int> permissionIds)
		{
			if (string.IsNullOrWhiteSpace(dto.RoleCode) || string.IsNullOrWhiteSpace(dto.RoleName) || permissionIds == null || permissionIds.Count == 0)
			{
				TempData["ErrorMessage"] = "建立失敗：請填寫完整資訊並至少勾選一項權限！";
				return RedirectToAction(nameof(Index), new { activeTab = "roles" });
			}

			int currentAdminId = 1;
			var result = _employeeService.CreateRole(dto, permissionIds, currentAdminId);

			SetTempDataMessage(result.Success, result.Message);
			return RedirectToAction(nameof(Index), new { activeTab = "roles" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult EditRole(int roleId, List<int> permissionIds)
		{
			if (permissionIds == null || permissionIds.Count == 0)
			{
				TempData["ErrorMessage"] = "修改失敗：一個角色至少必須包含一項權限！";
				return RedirectToAction(nameof(Index), new { activeTab = "roles" });
			}

			int currentAdminId = 1;
			var result = _employeeService.EditRole(roleId, permissionIds, currentAdminId);

			SetTempDataMessage(result.Success, result.Message);
			return RedirectToAction(nameof(Index), new { activeTab = "roles" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult DeleteRole(int roleId)
		{
			int currentAdminId = 1;
			var result = _employeeService.DeleteRole(roleId, currentAdminId);

			SetTempDataMessage(result.Success, result.Message);
			return RedirectToAction(nameof(Index), new { activeTab = "roles" });
		}

		// ==========================================
		// 🌟 權限字典 (Permissions) 管理 POST
		// ==========================================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult CreatePermission(PermissionDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto.PermissionCode) || string.IsNullOrWhiteSpace(dto.ModuleName) || string.IsNullOrWhiteSpace(dto.Description))
			{
				TempData["ErrorMessage"] = "建立失敗：所有欄位皆為必填！";
				return RedirectToAction(nameof(Index), new { activeTab = "permissions" });
			}

			int currentAdminId = 1;
			var result = _employeeService.CreatePermission(dto, currentAdminId);

			SetTempDataMessage(result.Success, result.Message);
			return RedirectToAction(nameof(Index), new { activeTab = "permissions" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult EditPermission(PermissionDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto.ModuleName) || string.IsNullOrWhiteSpace(dto.Description))
			{
				TempData["ErrorMessage"] = "修改失敗：請填寫模組名稱與中文描述！";
				return RedirectToAction(nameof(Index), new { activeTab = "permissions" });
			}

			int currentAdminId = 1;
			var result = _employeeService.EditPermission(dto, currentAdminId);

			SetTempDataMessage(result.Success, result.Message);
			return RedirectToAction(nameof(Index), new { activeTab = "permissions" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult DeletePermission(int permissionId)
		{
			int currentAdminId = 1;
			var result = _employeeService.DeletePermission(permissionId, currentAdminId);

			SetTempDataMessage(result.Success, result.Message);
			return RedirectToAction(nameof(Index), new { activeTab = "permissions" });
		}

		private void SetTempDataMessage(bool success, string message)
		{
			if (success) TempData["SuccessMessage"] = message;
			else TempData["ErrorMessage"] = message;
		}
	}
}