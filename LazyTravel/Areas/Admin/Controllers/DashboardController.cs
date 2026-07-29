using LazyTravel.Areas.Admin.Models;
using LazyTravel.Models;
using LazyTravel.Models.EfModels;
using LazyTravel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Areas.Admin.Controllers
{
	// 之後 Cookie 認證與 Role 授權建好後,改成:
	[Authorize(Policy = "RequireDashboardRead")]
	[Area("Admin")]
	public class DashboardController : Controller
	{
		// 故意用完整版的 LazyTravelDBContext,不用範圍只有 4 張表的舊版 LazyTravelContext——
		// 兩個是重複反向工程留下的產物,詳見團隊討論,總覽這邊先帶頭改成統一的那份
		private readonly LazyTravelDBContext _context;
		private readonly IReportService _reportService;
		private readonly IAdminLogService _adminLogService;

		public DashboardController(
			LazyTravelDBContext context,
			IReportService reportService,
			IAdminLogService adminLogService)
		{
			_context = context;
			_reportService = reportService;
			_adminLogService = adminLogService;
		}

		public async Task<IActionResult> Index()
		{
			ViewData["Title"] = "總覽";

			// 沒權限的模組連統計都不用查,直接省掉那一次 DB round trip
			bool Can(string permissionCode) => User.HasClaim("Permission", permissionCode);

			var memberCount = Can("member:account:read") ? await _context.Members.CountAsync() : 0;
			var activeGroupCount = Can("social:travelgroup:read") ? await _context.TravelGroups.CountAsync(g => !g.IsDelete) : 0;
			var publishedVlogCount = Can("content:vlog:read")
				? VlogPostStore.GetAll().Count(p => !p.IsDelete && p.Status == VlogPostStatus.Published)
				: 0;
			var pendingReportCount = Can("content:report:read")
				? _reportService.Query(new ReportQueryOptions()).PendingCount
				: 0;

			// 一次撈近期紀錄,再依 TargetTable 分流到各卡片自己的「最近動作」——
			// TravelGroups 目前沒有任何地方會寫入這個 log(TravelGroupsController 沒呼叫 WriteAsync),
			// 所以揪團管理卡會如實顯示空清單,不是漏寫
			var recentLogs = await _adminLogService.GetRecentAsync(50);
			List<DashboardLogEntry> LogsFor(string targetTable, int take) =>
				recentLogs
					.Where(log => log.TargetTable == targetTable)
					.OrderByDescending(log => log.CreatedAt)
					.Take(take)
					.Select(log => new DashboardLogEntry
					{
						CreatedAt = log.CreatedAt,
						Action = log.Action,
						Detail = log.Detail
					})
					.ToList();

			var viewModel = new DashboardViewModel
			{
				Modules = new List<DashboardModuleCard>
				{
					new()
					{
						Label = "會員管理",
						Href = Url.Action("Index", "Members")!,
						StatLabel = "位會員",
						StatValue = memberCount,
						ColorKey = "member",
						RequiredPermission = "member:account:read",
						RecentLogs = LogsFor("Members", 2)
					},
					new()
					{
						Label = "文章管理",
						Href = Url.Action("Index", "VlogPosts")!,
						StatLabel = "篇已發布",
						StatValue = publishedVlogCount,
						ColorKey = "article",
						RequiredPermission = "content:vlog:read",
						RecentLogs = LogsFor("VlogPosts", 2)
					},
					new()
					{
						Label = "揪團管理",
						Href = Url.Action("Index", "TravelGroups")!,
						StatLabel = "個揪團",
						StatValue = activeGroupCount,
						ColorKey = "group",
						RequiredPermission = "social:travelgroup:read",
						RecentLogs = LogsFor("TravelGroups", 2)
					},
					new()
					{
						Label = "檢舉管理",
						Href = Url.Action("Index", "Reports")!,
						StatLabel = "筆待處理",
						StatValue = pendingReportCount,
						Badge = pendingReportCount > 0 ? pendingReportCount : null,
						ColorKey = "report",
						RequiredPermission = "content:report:read",
						RecentLogs = LogsFor("Reports", 2)
					},
				}
			};

			viewModel.Modules = viewModel.Modules.Where(m => Can(m.RequiredPermission)).ToList();

			return View(viewModel);
		}
	}
}
