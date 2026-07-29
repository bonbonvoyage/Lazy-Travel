using LazyTravel.Models.DTOs;
using LazyTravel.Models.Services;
using LazyTravel.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace LazyTravel.Areas.Admin.Controllers
{
	[Area("Admin")]
	public class MembersController : Controller
	{
		private readonly IMemberService _memberService;

		public MembersController(IMemberService memberService)
		{
			_memberService = memberService;
		}

		// ==========================================
		// 1.1 會員管理主控制台 (整合雙頁籤功能)
		// ==========================================
		public IActionResult Index(
			// --- 必要參數 (無預設值) 必須放前面 ---
			string keyword, byte? status, byte? gender,
			string logAdmin, string logTarget, string logAction,
			// --- 選擇性參數 (有預設值) 必須放後面 ---
			int page = 1,
			int logPage = 1,
			string activeTab = "members")
		{
			int pageSize = 10;

			// --- 處理「會員管理」頁籤的資料 ---
			var memberResult = _memberService.GetAllMembers(keyword, status, gender, page);
			var memberVms = memberResult.Data.Select(dto => new MemberIndexVm
			{
				MemberID = dto.MemberID,
				Email = dto.Email,
				Name = dto.Name,
				GenderText = GetGenderText(dto.Gender),
				AgeText = dto.Age.HasValue ? $"{dto.Age}歲" : "-",
				Occupation = string.IsNullOrEmpty(dto.Occupation) ? "-" : dto.Occupation,
				MBTI = string.IsNullOrEmpty(dto.MBTI) ? "-" : dto.MBTI,
				PlanName = dto.PlanName,
				StatusText = dto.Status == 1 ? "正常" : "停權",
				StatusBadgeClass = dto.Status == 1 ? "bg-success" : "bg-danger",
				CreatedAtString = dto.CreatedAt.ToString("yyyy-MM-dd")
			}).ToList();

			int memberTotalPages = (int)Math.Ceiling(memberResult.TotalCount / (double)pageSize);

			// --- 處理「操作紀錄」頁籤的資料 ---
			var logResult = _memberService.GetMemberAdminLogs(logAdmin, logTarget, logAction, logPage);
			int logTotalPages = (int)Math.Ceiling(logResult.TotalCount / (double)pageSize);

			// --- 傳遞所有參數到前台 View ---
			ViewBag.ActiveTab = activeTab;

			// 會員分頁與篩選暫存
			ViewBag.Keyword = keyword;
			ViewBag.Status = status;
			ViewBag.Gender = gender;
			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = memberTotalPages;
			ViewBag.TotalCount = memberResult.TotalCount;

			// 操作紀錄分頁與篩選暫存
			ViewBag.LogAdmin = logAdmin;
			ViewBag.LogTarget = logTarget;
			ViewBag.LogAction = logAction;
			ViewBag.LogCurrentPage = logPage;
			ViewBag.LogTotalPages = logTotalPages;
			ViewBag.LogTotalCount = logResult.TotalCount;
			ViewBag.Logs = logResult.Data; // 直接將操作紀錄 DTO 列表傳給前台

			return View(memberVms);
		}

		// ==========================================
		// 1.2 檢視會員詳細資料 (含最新資安遮罩邏輯)
		// ==========================================
		public IActionResult Details(int id)
		{
			var dto = _memberService.GetMemberDetail(id);
			if (dto == null)
			{
				return NotFound();
			}

			int currentAdminRole = 1;

			if (currentAdminRole < 2)
			{
				if (!string.IsNullOrEmpty(dto.Phone) && dto.Phone.Length >= 10)
				{
					dto.Phone = $"{dto.Phone.Substring(0, 4)}-***-{dto.Phone.Substring(dto.Phone.Length - 3)}";
				}

				if (!string.IsNullOrEmpty(dto.LineId))
				{
					dto.LineId = dto.LineId.Length > 3 ? $"{dto.LineId.Substring(0, 3)}***" : "***";
				}

				if (!string.IsNullOrEmpty(dto.InstagramUrl))
				{
					dto.InstagramUrl = "instagram.com/***";
				}

				if (!string.IsNullOrEmpty(dto.FacebookUrl))
				{
					dto.FacebookUrl = "facebook.com/***";
				}
			}

			var editDto = new MemberEditDto
			{
				MemberID = dto.MemberID,
				Status = dto.Status,
				ResetName = false,
				ResetBio = false,
				RemoveAvatar = false,
				SendNotification = false
			};

			ViewBag.EditDto = editDto;

			return View(dto);
		}

		// ==========================================
		// 1.3 儲存會員編輯資料
		// ==========================================
		[HttpPost]
		[ValidateAntiForgeryToken]
		// 🌟 恢復優雅的 Model Binding
		public IActionResult Edit(MemberEditDto editDto)
		{
			// 防呆：如果 Model 驗證失敗，直接退回
			if (!ModelState.IsValid)
			{
				TempData["ErrorMessage"] = "資料格式錯誤，請重新確認並填寫所有必填欄位。";
				return RedirectToAction(nameof(Details), new { id = editDto.MemberID });
			}

			// 執行資料庫更新邏輯
			bool isSuccess = _memberService.EditMember(editDto);

			if (isSuccess)
			{
				TempData["SuccessMessage"] = "會員資料處分成功，已自動寫入此模組的操作紀錄！";
			}
			else
			{
				TempData["ErrorMessage"] = "會員資料更新失敗，系統找不到該會員。";
			}

			return RedirectToAction(nameof(Details), new { id = editDto.MemberID });
		}

		private string GetGenderText(byte gender)
		{
			return gender switch
			{
				1 => "男",
				2 => "女",
				3 => "其他",
				_ => "未知"
			};
		}
	}
}