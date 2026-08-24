using LazyTravel.Models.EfModels;
using LazyTravel.Shared.ViewModels;
using LazyTravel.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LazyTravel.Areas.Admin.Controllers
{
	[Area("Admin")]
	public class TravelGroupsController : Controller
	{
		// 改用完整版的 LazyTravelDBContext,不用範圍只有 4 張表、即將淘汰的 LazyTravelContext
		private readonly LazyTravelDBContext _context;

		// 審核狀態篩選僅開放這 3 種（配合前台簡化後的下拉選單）
		private static readonly string[] AllowedReviewStatuses =
		{
			"正常", "檢舉審核中", "違規"
		};

		private static readonly string[] AbnormalReviewStatuses =
		{
			"檢舉審核中", "違規"
		};

		public TravelGroupsController(LazyTravelDBContext context)
		{
			_context = context;
		}


		public async Task<IActionResult> Index(
		   string? keyword,
		   DateTime? createdAtFrom,
		   DateTime? createdAtTo,
		   string? selectedReviewStatus,
		   string sortOrder = "asc",
		   int activePage = 1,
		   int abnormalPage = 1,
		   int deletedPage = 1,
		   int logPage = 1,
		   string tab = "active")
		{
			int pageSize = 10;

			if (!string.IsNullOrWhiteSpace(selectedReviewStatus) &&
				!AllowedReviewStatuses.Contains(selectedReviewStatus))
			{
				selectedReviewStatus = null;
			}

			await TGReviewStatusService.SyncAsync(_context);

			var query = _context.TravelGroups
				.Include(g => g.OwnerMember)
				.AsQueryable();

			// 關鍵字搜尋：編號、房間名稱、說明、國家、地區、團主
			if (!string.IsNullOrWhiteSpace(keyword))
			{
				bool isNumber = int.TryParse(keyword, out int groupId);

				query = query.Where(g =>
					(isNumber && g.GroupId == groupId) ||
					g.GroupTitle.Contains(keyword) ||
					(g.Description != null && g.Description.Contains(keyword)) ||
					(g.Country != null && g.Country.Contains(keyword)) ||
					(g.Region != null && g.Region.Contains(keyword)) ||
					(g.OwnerMember != null && g.OwnerMember.Name.Contains(keyword)));
			}

			//建立時間起迄
			if (createdAtFrom.HasValue)
			{
				query = query.Where(g => g.CreatedAt >= createdAtFrom.Value);
			}

			if (createdAtTo.HasValue)
			{
				var endDate = createdAtTo.Value.Date.AddDays(1).AddTicks(-1);
				query = query.Where(g => g.CreatedAt <= endDate);
			}



			// 審核狀態單選（唯一保留的篩選分類）
			if (!string.IsNullOrWhiteSpace(selectedReviewStatus))
			{
				query = query.Where(g => g.ReviewStatus == selectedReviewStatus);
			}

			var activeQuery = query.Where(g => g.IsDelete == false);
			var abnormalQuery = query.Where(g => g.IsDelete == false && AbnormalReviewStatuses.Contains(g.ReviewStatus));
			var deletedQuery = query.Where(g => g.IsDelete == true);

			// 排序：預設編號小到大
			if (sortOrder == "desc")
			{
				activeQuery = activeQuery.OrderByDescending(g => g.GroupId);
				abnormalQuery = abnormalQuery.OrderByDescending(g => g.GroupId);
				deletedQuery = deletedQuery.OrderByDescending(g => g.GroupId);
			}
			else
			{
				activeQuery = activeQuery.OrderBy(g => g.GroupId);
				abnormalQuery = abnormalQuery.OrderBy(g => g.GroupId);
				deletedQuery = deletedQuery.OrderBy(g => g.GroupId);
			}

			if (activePage < 1)
			{
				activePage = 1;
			}

			if (abnormalPage < 1)
			{
				abnormalPage = 1;
			}

			if (deletedPage < 1)
			{
				deletedPage = 1;
			}

			if (logPage < 1)
			{
				logPage = 1;
			}

			var activeTotalCount = await activeQuery.CountAsync();
			var abnormalTotalCount = await abnormalQuery.CountAsync();
			var deletedTotalCount = await deletedQuery.CountAsync();

			// 異動紀錄清單：依建立時間新到舊排序，不套用揪團篩選條件（獨立瀏覽異動歷程）
			var logQuery = _context.TravelGroupsLogs
				.Include(l => l.Group)
				.OrderByDescending(l => l.CreatedAt)
				.AsQueryable();

			var logTotalCount = await logQuery.CountAsync();
			var logs = await logQuery
				.Skip((logPage - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var logEmployeeIds = logs
				.Where(l => l.ChangedByEmployeeId.HasValue)
				.Select(l => l.ChangedByEmployeeId!.Value)
				.Distinct()
				.ToList();

			var logEmployeeNames = logEmployeeIds.Count == 0
				? new Dictionary<int, string>()
				: await _context.Employees
					.Where(e => logEmployeeIds.Contains(e.EmployeeId))
					.ToDictionaryAsync(e => e.EmployeeId, e => e.Name);

			var vm = new TravelGroupAdminIndexViewModel
			{
				ActiveGroups = await activeQuery
					.Skip((activePage - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(),

				AbnormalGroups = await abnormalQuery
					.Skip((abnormalPage - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(),

				DeletedGroups = await deletedQuery
					.Skip((deletedPage - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(),

				Logs = logs,
				LogEmployeeNames = logEmployeeNames,

				Keyword = keyword,
				CreatedAtFrom = createdAtFrom,
				CreatedAtTo = createdAtTo,

				SelectedReviewStatus = selectedReviewStatus,

				SortOrder = sortOrder,

				ActivePage = activePage,
				AbnormalPage = abnormalPage,
				DeletedPage = deletedPage,
				LogPage = logPage,

				ActiveTotalPages = (int)Math.Ceiling(activeTotalCount / (double)pageSize),
				AbnormalTotalPages = (int)Math.Ceiling(abnormalTotalCount / (double)pageSize),
				DeletedTotalPages = (int)Math.Ceiling(deletedTotalCount / (double)pageSize),
				LogTotalPages = (int)Math.Ceiling(logTotalCount / (double)pageSize),

				PageSize = pageSize,
				Tab = tab,

				TotalGroupsCount = activeTotalCount,
				AbnormalGroupsCount = abnormalTotalCount,
				DeletedGroupsCount = deletedTotalCount,
				LogsCount = logTotalCount
			};

			// AJAX 局部更新：只回傳頁籤與清單區塊，不重整整頁
			if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
			{
				return PartialView("_TravelGroupsTabContent", vm);
			}

			return View(vm);
		}

		public async Task<IActionResult> Details(int id)
		{
			var group = await FindGroupDetailsAsync(id);

			if (group == null)
			{
				return NotFound();
			}

			ViewBag.OperationEmployeeNames = await GetOperationEmployeeNamesAsync(group.TravelGroupsLogs);
			return View(group);
		}

		public async Task<IActionResult> DetailsPanel(int id)
		{
			var group = await FindGroupDetailsAsync(id);

			if (group == null)
			{
				return NotFound();
			}

			ViewBag.OperationEmployeeNames = await GetOperationEmployeeNamesAsync(group.TravelGroupsLogs);
			return PartialView("_TravelGroupDetailsContent", group);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id)
		{
			var group = await _context.TravelGroups
				.FirstOrDefaultAsync(g => g.GroupId == id);

			if (group == null)
			{
				return NotFound();
			}

			// 只有已判定違規的揪團才允許後台軟刪除
			if (group.ReviewStatus != "違規")
			{
				TempData["ErrorMessage"] = "只有已判定違規的揪團可以刪除。";
				return RedirectToAction(nameof(Index), new { tab = "active" });
			}

			group.IsDelete = true;
			group.IsPublic = false;
			group.UpdatedAt = DateTime.Now;

			var reportReason = NormalizeOperationRemark(await GetLatestTravelGroupReportReasonAsync(group.GroupId));

			_context.TravelGroupsLogs.Add(CreateLog(
				group,
				"刪除",
				"IsDelete",
				"0",
				"1",
				string.IsNullOrWhiteSpace(reportReason)
					? $"審核狀態為「{group.ReviewStatus}」，移至已刪除清單。"
					: reportReason));

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "揪團已移至已刪除清單。";
			return RedirectToAction(nameof(Index), new { tab = "deleted" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Restore(int id, string? operationRemark)
		{
			operationRemark = NormalizeOperationRemark(operationRemark);
			if (string.IsNullOrWhiteSpace(operationRemark))
			{
				TempData["ErrorMessage"] = "還原揪團前，請填寫操作異動說明。";
				return RedirectToAction(nameof(Index), new { tab = "deleted" });
			}

			var group = await _context.TravelGroups
				.FirstOrDefaultAsync(g => g.GroupId == id);

			if (group == null)
			{
				return NotFound();
			}

			var oldReviewStatus = group.ReviewStatus;

			// 還原代表從已刪除清單移回所有揪團清單，審核狀態回復正常
			group.IsDelete = false;
			group.ReviewStatus = "正常";
			group.IsPublic = true;
			group.UpdatedAt = DateTime.Now;

			_context.TravelGroupsLogs.Add(CreateLog(
				group,
				"還原",
				"ReviewStatus",
				oldReviewStatus,
				"正常",
				operationRemark));

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "揪團已還原至正常清單。";
			return RedirectToAction(nameof(Index), new { tab = "active" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SaveOperationRemark(int id, string? operationRemark)
		{
			operationRemark = NormalizeOperationRemark(operationRemark);
			if (string.IsNullOrWhiteSpace(operationRemark))
			{
				TempData["ErrorMessage"] = "請填寫操作異動說明。";
				return RedirectToAction(nameof(Index), new { tab = "log" });
			}

			var group = await _context.TravelGroups
				.FirstOrDefaultAsync(g => g.GroupId == id);

			if (group == null)
			{
				return NotFound();
			}

			_context.TravelGroupsLogs.Add(CreateLog(
				group,
				"新增說明",
				"Remark",
				"",
				operationRemark,
				operationRemark));

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "操作異動說明已儲存。";
			if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
			{
				var refreshedGroup = await FindGroupDetailsAsync(id);
				if (refreshedGroup == null)
				{
					return NotFound();
				}

				ViewBag.OperationEmployeeNames = await GetOperationEmployeeNamesAsync(refreshedGroup.TravelGroupsLogs);
				return PartialView("_TravelGroupDetailsContent", refreshedGroup);
			}

			return RedirectToAction(nameof(Index), new { tab = "log" });
		}

		private TravelGroupsLog CreateLog(
			TravelGroup group,
			string changeType,
			string fieldName,
			string oldValue,
			string newValue,
			string remark)
		{
			return new TravelGroupsLog
			{
				GroupId = group.GroupId,
				ChangedByEmployeeId = GetCurrentEmployeeId(),
				ChangeType = changeType,
				FieldName = fieldName,
				OldValue = oldValue,
				NewValue = newValue,
				Remark = remark,
				CreatedAt = DateTime.Now
			};
		}

		private async Task<string?> GetLatestTravelGroupReportReasonAsync(int groupId)
		{
			return await _context.Reports
				.Where(r => r.ReportType == 4 && r.TargetId == groupId)
				.OrderByDescending(r => r.CreatedAt)
				.Select(r => r.Reason)
				.FirstOrDefaultAsync();
		}

		private static string? NormalizeOperationRemark(string? remark)
		{
			if (string.IsNullOrWhiteSpace(remark))
			{
				return null;
			}

			remark = remark.Trim();
			return remark.Length <= 200 ? remark : remark[..200];
		}

		private int GetCurrentEmployeeId()
		{
			var claimKeys = new[]
			{
				ClaimTypes.NameIdentifier,
				"EmployeeId",
				"EmployeeID",
				"AdminId",
				"AdminID"
			};

			foreach (var key in claimKeys)
			{
				var value = User.FindFirstValue(key);
				if (int.TryParse(value, out var employeeId))
				{
					return employeeId;
				}
			}

			var sessionKeys = new[]
			{
				"EmployeeId",
				"EmployeeID",
				"AdminId",
				"AdminID"
			};

			foreach (var key in sessionKeys)
			{
				try
				{
					var employeeId = HttpContext.Session.GetInt32(key);
					if (employeeId.HasValue)
					{
						return employeeId.Value;
					}
				}
				catch (InvalidOperationException)
				{
					break;
				}
			}

			return 1;
		}

		private async Task<Dictionary<int, string>> GetOperationEmployeeNamesAsync(IEnumerable<TravelGroupsLog>? logs)
		{
			var employeeIds = (logs ?? Enumerable.Empty<TravelGroupsLog>())
				.Where(l => l.ChangedByEmployeeId.HasValue)
				.Select(l => l.ChangedByEmployeeId!.Value)
				.Distinct()
				.ToList();

			return employeeIds.Count == 0
				? new Dictionary<int, string>()
				: await _context.Employees
					.Where(e => employeeIds.Contains(e.EmployeeId))
					.ToDictionaryAsync(e => e.EmployeeId, e => e.Name);
		}

		private Task<TravelGroup?> FindGroupDetailsAsync(int id)
		{
			return _context.TravelGroups
				.Include(g => g.OwnerMember)
				.Include(g => g.GroupMembers)
					.ThenInclude(gm => gm.Member)
				.Include(g => g.TravelGroupsLogs)
				.FirstOrDefaultAsync(g => g.GroupId == id);
		}
	}
}
