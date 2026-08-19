using LazyTravel.Models.DTOs;
using LazyTravel.Models.ViewModels;
using LazyTravel.Services;
using Microsoft.AspNetCore.Authorization;
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
		[Authorize(Policy = "RequireMemberRead")] // 🌟 加入門禁：必須要有讀取權限才能進入列表
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
		[Authorize(Policy = "RequireMemberRead")]
		public IActionResult Details(int id)
		{
			var dto = _memberService.GetMemberDetail(id);
			if (dto == null)
			{
				return NotFound();
			}

			// 🌟 改變邏輯：不管你是誰，初始載入畫面時「一律強制遮蔽」！
			// 必須透過畫面上的按鈕發送 AJAX 請求才能解鎖並留存紀錄。
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
		// 🌟 新增 API：供前端 AJAX 呼叫以解除遮蔽並寫入紀錄
		// ==========================================
		[HttpPost]
		// 🌟 刪除 Route 標籤，讓系統預設路由處理
		[Authorize(Policy = "RequireMemberRead")] // 基本門禁
		[ValidateAntiForgeryToken] // 防止 CSRF 攻擊
		public IActionResult Unmask(int id)
		{
			// 雙重檢查：確認該員工真的有「解除遮蔽」的細粒度權限
			if (!User.HasClaim("Permission", "member:pii:unmask") && !User.HasClaim("Permission", "system:employee:manage"))
			{
				return Unauthorized(new { message = "權限不足，無法調閱個資" });
			}

			// 從資料庫取得「未遮蔽」的真實資料
			var realDto = _memberService.GetMemberDetail(id);
			if (realDto == null) return NotFound();

			// 取得目前操作員工的 ID 與 IP
			int currentAdminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "1");
			string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

			// 寫入調閱日誌！
			_memberService.LogPiiUnmask(id, currentAdminId, ipAddress);

			// 把真實資料回傳給前端的 AJAX
			return Json(new
			{
				success = true,
				phone = string.IsNullOrEmpty(realDto.Phone) ? "未填寫" : realDto.Phone,
				lineId = string.IsNullOrEmpty(realDto.LineId) ? "未綁定" : realDto.LineId,
				ig = string.IsNullOrEmpty(realDto.InstagramUrl) ? "未綁定" : realDto.InstagramUrl,
				fb = string.IsNullOrEmpty(realDto.FacebookUrl) ? "未綁定" : realDto.FacebookUrl
			});
		}

		// ==========================================
		// 1.3 儲存會員編輯資料
		// ==========================================
		[Authorize(Policy = "RequireMemberBlock")] // 🌟 核心防護：確保就算駭客猜到這支 API 的網址，沒有權限也無法發送 POST 請求停權別人！
		[HttpPost]
		[ValidateAntiForgeryToken]
		// 🌟 恢復優雅的 Model Binding
		public IActionResult Edit(MemberEditDto editDto)
		{
			// 會員編號沒綁到就別再往下走：Details/0 是不存在的頁面，錯誤會被藏在一個看不懂的畫面裡
			if (editDto.MemberID <= 0)
			{
				TempData["ErrorMessage"] = "表單沒有帶到會員編號，處分未執行。請重新整理該會員頁面後再試一次。";
				return RedirectToAction(nameof(Index));
			}

			// 防呆：如果 Model 驗證失敗，直接退回
			if (!ModelState.IsValid)
			{
				TempData["ErrorMessage"] = "資料格式錯誤，請重新確認並填寫所有必填欄位。";
				return RedirectToAction(nameof(Details), new { id = editDto.MemberID });
			}

			// 操作人取自 Cookie 的員工識別證,不要寫死——稽核紀錄要對得上真正動手的人
			if (!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out int currentAdminId))
			{
				TempData["ErrorMessage"] = "無法識別操作人身分，處分未執行。請重新登入後再試一次。";
				return RedirectToAction(nameof(Details), new { id = editDto.MemberID });
			}

			// 執行資料庫更新邏輯
			bool isSuccess = _memberService.EditMember(editDto, currentAdminId);

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