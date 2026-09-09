using LazyTravel.Models;
using Microsoft.EntityFrameworkCore;
using EfAdminLog = LazyTravel.Models.EfModels.AdminLog;
using EfMember = LazyTravel.Models.EfModels.Member;
using LazyTravelDBContext = LazyTravel.Models.EfModels.LazyTravelDBContext;

namespace LazyTravel.Services
{
	// 寫進官方規格書的 dbo.AdminLogs 資料表（透過 LazyTravelDBContext.AdminLogs），取代原本重開網站
	// 就會消失的記憶體版本。AdminLogs.AdminID 是外鍵，一定要指向 Members 裡真實存在的一筆資料，
	// 但專案目前還沒有真的 Cookie 登入系統，沒辦法知道「現在是哪個會員在操作後台」，
	// 而後台登入的是 Employees 不是 Members，所以用員工 Email 在 Members 找/建一筆對應的會員來當外鍵目標
	// (見 ResolveAdminIdAsync)；對不到員工時才退回固定的系統管理員帳號(SystemAdminEmail)當佔位。
	public class AdminLogService : IAdminLogService
	{
		private const string SystemAdminEmail = "system-admin@lazytravel.local";

		private readonly LazyTravelDBContext _context;

		public AdminLogService(LazyTravelDBContext context)
		{
			_context = context;
		}

		public async Task WriteAsync(string operatorName, string action, string detail, string? targetTable = null, int? targetId = null)
		{
			var adminId = await ResolveAdminIdAsync(operatorName);

			_context.AdminLogs.Add(new EfAdminLog
			{
				AdminId = adminId,
				Action = action,
				TargetTable = targetTable ?? "Unknown",
				TargetId = targetId,
				Description = detail,
				IPAddress = "127.0.0.1",
				CreatedAt = DateTime.Now,
			});

			await _context.SaveChangesAsync();
		}

		public async Task<List<AdminLog>> GetRecentAsync(int take = 50)
		{
			var logs = await _context.AdminLogs
				.AsNoTracking()
				.Include(l => l.Admin)
				.OrderByDescending(l => l.CreatedAt)
				.Take(take)
				.ToListAsync();

			var employeeNames = await GetEmployeeNamesByEmailAsync(logs);
			return logs.Select(l => ToDto(l, employeeNames)).ToList();
		}

		public async Task<List<AdminLog>> GetForTargetAsync(string targetTable, int targetId)
		{
			var logs = await _context.AdminLogs
				.AsNoTracking()
				.Include(l => l.Admin)
				.Where(l => l.TargetTable == targetTable && l.TargetId == targetId)
				.OrderByDescending(l => l.CreatedAt)
				.ToListAsync();

			var employeeNames = await GetEmployeeNamesByEmailAsync(logs);
			return logs.Select(l => ToDto(l, employeeNames)).ToList();
		}

		// 篩選 + 分頁查詢,給檢舉審核台的操作紀錄分頁用。
		// 用「動作名稱」而不是 TargetTable 界定範圍:檢舉模組寫的紀錄裡,「自動停權」這類是記在
		// TargetTable="Members"(因為改動的是會員狀態),不是 "Reports",用 TargetTable 篩會漏掉。
		public async Task<(List<AdminLog> Data, int TotalCount)> QueryAsync(
			IEnumerable<string> actionScope, string? operatorKeyword, string? detailKeyword, string? action, int page, int pageSize = 10)
		{
			var scopeList = actionScope.ToList();
			var query = _context.AdminLogs
				.AsNoTracking()
				.Include(l => l.Admin)
				.Where(l => scopeList.Contains(l.Action));

			if (!string.IsNullOrWhiteSpace(operatorKeyword))
			{
				// 畫面上「執行管理員」顯示的是 Employees 姓名(見 ToDto),搜尋邏輯也要對得起來,
				// 不然使用者照畫面打員工姓名(例如「浚翔」)會查不到東西。
				// 用 Email 反查符合關鍵字的員工,再用 Email 比對 Admin,同時保留原本比對 Member.Name
				// 的邏輯(查不到對應員工姓名時,畫面退回顯示 Member 姓名,搜尋也要能對得上)。
				var matchingEmails = await _context.Employees
					.AsNoTracking()
					.Where(e => e.Name.Contains(operatorKeyword))
					.Select(e => e.Email)
					.ToListAsync();

				query = query.Where(l => l.Admin != null &&
					(l.Admin.Name.Contains(operatorKeyword) || matchingEmails.Contains(l.Admin.Email)));
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

			var employeeNames = await GetEmployeeNamesByEmailAsync(logs);
			var result = logs.Select(l => ToDto(l, employeeNames)).ToList();

			// Description 是自由文字,資料庫端查不了關鍵字,拿到這一頁的資料後在記憶體裡篩
			if (!string.IsNullOrWhiteSpace(detailKeyword))
			{
				result = result.Where(r => r.Detail.Contains(detailKeyword, StringComparison.OrdinalIgnoreCase)).ToList();
				totalCount = result.Count;
			}

			return (result, totalCount);
		}

		// 「審核人員」對外顯示要優先用 Employees 表的姓名(真實員工身分),不是 Members 表的暱稱。
		// AdminLogs.AdminID 這個外鍵本身還是照組長定案指向 Members,這裡只在顯示這一步,
		// 用 Member 的 Email 去反查 Employees 裡對應的員工姓名;查不到就退回原本的 Member 姓名。
		private static AdminLog ToDto(EfAdminLog log, IReadOnlyDictionary<string, string> employeeNamesByEmail)
		{
			string? displayName = null;
			if (log.Admin?.Email is { } email && employeeNamesByEmail.TryGetValue(email, out var employeeName))
			{
				displayName = employeeName;
			}

			return new()
			{
				Id = log.LogId,
				OperatorName = displayName ?? log.Admin?.Name ?? "系統管理員",
				Action = log.Action,
				TargetTable = log.TargetTable,
				TargetId = log.TargetId,
				Detail = log.Description ?? string.Empty,
				CreatedAt = log.CreatedAt,
			};
		}

		private async Task<IReadOnlyDictionary<string, string>> GetEmployeeNamesByEmailAsync(List<EfAdminLog> logs)
		{
			var emails = logs
				.Where(l => l.Admin != null)
				.Select(l => l.Admin!.Email)
				.Distinct()
				.ToList();

			if (emails.Count == 0)
			{
				return new Dictionary<string, string>();
			}

			return await _context.Employees
				.AsNoTracking()
				.Where(e => emails.Contains(e.Email))
				.ToDictionaryAsync(e => e.Email, e => e.Name);
		}

		// operatorName 是登入員工的姓名(User.Identity.Name,見 Admin/AuthController 寫進 ClaimTypes.Name)。
		// AdminLogs.AdminID 這個外鍵只能指向 Members,所以拿員工的 Email 在 Members 找/建一筆同 Email 的
		// 會員,當這位員工在操作紀錄裡的身分;顯示時 ToDto 就能用這個 Email 反查回 Employees 姓名。
		// 對不到員工(還沒登入、或是前台會員自己的動作)才退回原本的系統管理員佔位。
		// ponytail: 用姓名反查員工,同名員工會撞在一起;之後 WriteAsync 改成直接收 Claims 裡的 Email 就沒這問題。
		private async Task<int> ResolveAdminIdAsync(string? operatorName)
		{
			string? employeeEmail = null;
			if (!string.IsNullOrWhiteSpace(operatorName))
			{
				employeeEmail = await _context.Employees
					.AsNoTracking()
					.Where(e => e.Name == operatorName)
					.Select(e => e.Email)
					.FirstOrDefaultAsync();
			}

			return employeeEmail is null
				? await ResolveMemberIdByEmailAsync(SystemAdminEmail, "系統管理員")
				: await ResolveMemberIdByEmailAsync(employeeEmail, operatorName!);
		}

		private async Task<int> ResolveMemberIdByEmailAsync(string email, string name)
		{
			var existing = await _context.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Email == email);
			if (existing is not null)
			{
				return existing.MemberId;
			}

			var placeholder = new EfMember
			{
				Email = email,
				Name = name,
				CreatedAt = DateTime.Now,
			};

			try
			{
				_context.Members.Add(placeholder);
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateException)
			{
				// 極少數同時搶著建立同一筆的情況：Email 唯一鍵會擋下重複插入，改成查已經存在的那筆。
				_context.Entry(placeholder).State = EntityState.Detached;
				var raceWinner = await _context.Members.AsNoTracking().FirstAsync(m => m.Email == email);
				return raceWinner.MemberId;
			}

			return placeholder.MemberId;
		}
	}
}