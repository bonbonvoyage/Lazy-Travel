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
    // 所以先用固定的系統管理員帳號(SystemAdminEmail)當全站操作紀錄共用的操作人佔位，
    // 第一次用的時候如果資料庫還沒有這筆資料就自動建立一筆。
    // 之後接上真的登入系統後，把 ResolveSystemAdminIdAsync 換成從目前登入者的 Claims 拿 MemberID 即可。
    public class AdminLogService : IAdminLogService
    {
        private const string SystemAdminEmail = "system-admin@lazytravel.local";

        private static int? _cachedSystemAdminId;

        private readonly LazyTravelDBContext _context;

        public AdminLogService(LazyTravelDBContext context)
        {
            _context = context;
        }

        public async Task WriteAsync(string operatorName, string action, string detail, string? targetTable = null, int? targetId = null)
        {
            var adminId = await ResolveSystemAdminIdAsync();

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

            return logs.Select(ToDto).ToList();
        }

        public async Task<List<AdminLog>> GetForTargetAsync(string targetTable, int targetId)
        {
            var logs = await _context.AdminLogs
                .AsNoTracking()
                .Include(l => l.Admin)
                .Where(l => l.TargetTable == targetTable && l.TargetId == targetId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return logs.Select(ToDto).ToList();
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
                query = query.Where(l => l.Admin != null && l.Admin.Name.Contains(operatorKeyword));
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
                result = result.Where(r => r.Detail.Contains(detailKeyword, StringComparison.OrdinalIgnoreCase)).ToList();
                totalCount = result.Count;
            }

            return (result, totalCount);
        }

        private static AdminLog ToDto(EfAdminLog log) => new()
        {
            Id = log.LogId,
            OperatorName = log.Admin?.Name ?? "系統管理員",
            Action = log.Action,
            TargetTable = log.TargetTable,
            TargetId = log.TargetId,
            Detail = log.Description ?? string.Empty,
            CreatedAt = log.CreatedAt,
        };

        private async Task<int> ResolveSystemAdminIdAsync()
        {
            if (_cachedSystemAdminId.HasValue)
            {
                return _cachedSystemAdminId.Value;
            }

            var existing = await _context.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Email == SystemAdminEmail);
            if (existing is not null)
            {
                _cachedSystemAdminId = existing.MemberId;
                return existing.MemberId;
            }

            var placeholder = new EfMember
            {
                Email = SystemAdminEmail,
                Name = "系統管理員",
                CreatedAt = DateTime.Now,
            };

            try
            {
                _context.Members.Add(placeholder);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // 極少數同時搶著建立第一筆的情況：Email 唯一鍵會擋下重複插入，改成查已經存在的那筆。
                _context.Entry(placeholder).State = EntityState.Detached;
                var raceWinner = await _context.Members.AsNoTracking().FirstAsync(m => m.Email == SystemAdminEmail);
                _cachedSystemAdminId = raceWinner.MemberId;
                return raceWinner.MemberId;
            }

            _cachedSystemAdminId = placeholder.MemberId;
            return placeholder.MemberId;
        }
    }
}
