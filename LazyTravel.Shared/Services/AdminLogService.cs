using LazyTravel.Shared.Models.DTOs;
using LazyTravel.Shared.Models.EfModels;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Shared.Services
{
	// 寫進官方規格書的 dbo.AdminAuditLogs 資料表(透過 LazyTravelDBContext.AdminAuditLogs)。
	// AdminAuditLogs.EmployeeId 直接對應登入員工的真實 EmployeeID(Admin/AuthController 登入時寫進
	// ClaimTypes.NameIdentifier 的那個值),呼叫端一律要傳真正的 employeeId,不再像舊版 AdminLogs
	// 那樣借道 Members 表建一筆佔位會員來當外鍵目標。
	public class AdminLogService : IAdminLogService
	{
		private readonly LazyTravelDBContext _context;

		public AdminLogService(LazyTravelDBContext context)
		{
			_context = context;
		}

		public async Task WriteAsync(int employeeId, string action, string detail, string? targetResource = null, string? targetId = null)
		{
			_context.AdminAuditLogs.Add(new AdminAuditLog
			{
				EmployeeId = employeeId,
				Action = action,
				TargetResource = targetResource ?? "Unknown",
				TargetId = targetId,
				Description = detail,
				Ipaddress = "127.0.0.1",
				CreatedAt = DateTime.Now,
			});

			await _context.SaveChangesAsync();
		}

		public async Task<List<AdminLogDto>> GetRecentAsync(int take = 50)
		{
			var logs = await _context.AdminAuditLogs
				.AsNoTracking()
				.Include(l => l.Employee)
				.OrderByDescending(l => l.CreatedAt)
				.Take(take)
				.ToListAsync();

			return logs.Select(ToDto).ToList();
		}

		public async Task<List<AdminLogDto>> GetForTargetAsync(string targetResource, string targetId)
		{
			var logs = await _context.AdminAuditLogs
				.AsNoTracking()
				.Include(l => l.Employee)
				.Where(l => l.TargetResource == targetResource && l.TargetId == targetId)
				.OrderByDescending(l => l.CreatedAt)
				.ToListAsync();

			return logs.Select(ToDto).ToList();
		}

		// 篩選 + 分頁查詢,給檢舉審核台的操作紀錄分頁用。
		// 用「動作名稱」而不是 TargetResource 界定範圍:檢舉模組寫的紀錄裡,「自動停權」這類是記在
		// TargetResource="Members"(因為改動的是會員狀態),不是 "Reports",用 TargetResource 篩會漏掉。
		public async Task<(List<AdminLogDto> Data, int TotalCount)> QueryAsync(
			IEnumerable<string> actionScope, string? operatorKeyword, string? detailKeyword, string? action, int page, int pageSize = 10)
		{
			var scopeList = actionScope.ToList();
			var query = _context.AdminAuditLogs
				.AsNoTracking()
				.Include(l => l.Employee)
				.Where(l => scopeList.Contains(l.Action));

			if (!string.IsNullOrWhiteSpace(operatorKeyword))
			{
				query = query.Where(l => l.Employee != null && l.Employee.Name.Contains(operatorKeyword));
			}

			if (!string.IsNullOrWhiteSpace(action))
			{
				query = query.Where(l => l.Action == action);
			}

			int totalCount = await query.CountAsync();

			var logs = await query
				.OrderByDescending(l => l.CreatedAt)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var result = logs.Select(ToDto).ToList();

			// Description 是自由文字,資料庫端查不了關鍵字,拿到這一頁的資料後在記憶體裡篩
			if (!string.IsNullOrWhiteSpace(detailKeyword))
			{
				result = result.Where(r => r.Description.Contains(detailKeyword, StringComparison.OrdinalIgnoreCase)).ToList();
				totalCount = result.Count;
			}

			return (result, totalCount);
		}

		private static AdminLogDto ToDto(AdminAuditLog log) => new()
		{
			LogID = log.LogId,
			EmployeeID = log.EmployeeId,
			AdminName = log.Employee?.Name ?? "系統",
			Action = log.Action,
			TargetResource = log.TargetResource,
			TargetID = log.TargetId,
			TargetMemberName = string.Empty,
			Description = log.Description ?? string.Empty,
			IPAddress = log.Ipaddress,
			CreatedAt = log.CreatedAt,
		};
	}
}
