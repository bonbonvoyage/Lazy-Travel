using LazyTravel.Models.EfModels;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Services
{
    // 已接上 10 黃浚翔整合進來的真實 AdminLogs 資料表(2026-07-22),不再是只存在記憶體的暫時實作。
    // 前台還沒有登入系統,目前沒有真的「當前登入管理員」可以拿,AdminID 先透過姓名反查 Employees 表對應的員工,
    // 等 Cookie 登入做好後,這裡要改成從當前登入者的 Claims 直接抓真正的 EmployeeID。
    // 這個 Service 同時被檢舉審核台(ReportService)跟 Vlog 行程文章(VlogPostsController)共用。
    public class AdminLogService : IAdminLogService
    {
        private readonly LazyTravelDBContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminLogService(LazyTravelDBContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task WriteAsync(string operatorName, string action, string detail, string? targetTable = null, int? targetId = null)
        {
            var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

            // operatorName 理想上是真的員工姓名(例如 ReportService 隨機挑的員工),用姓名反查 Employees 表拿到真正的 EmployeeID;
            // 查不到時(例如尚未接 Cookie 登入的呼叫端傳的是 "管理員" 這種預設字串)退回第一位員工,避免寫入失敗
            var adminId = await _context.Employees
                .Where(e => e.Name == operatorName)
                .Select(e => e.EmployeeId)
                .FirstOrDefaultAsync();

            if (adminId == 0)
            {
                adminId = await _context.Employees.OrderBy(e => e.EmployeeId).Select(e => e.EmployeeId).FirstOrDefaultAsync();
            }

            _context.AdminLogs.Add(new AdminLog
            {
                AdminId = adminId,
                Action = action,
                TargetTable = targetTable ?? string.Empty,
                TargetId = targetId,
                // 操作人姓名同時併入描述文字,方便直接閱讀,不用額外 join
                Description = $"[{operatorName}] {detail}",
                Ipaddress = ipAddress,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        public async Task<List<Models.AdminLog>> GetRecentAsync(int take = 50)
        {
            var logs = await _context.AdminLogs
                .Include(l => l.Admin)
                .AsNoTracking()
                .OrderByDescending(l => l.CreatedAt)
                .Take(take)
                .ToListAsync();

            return logs.Select(ToReportsModel).ToList();
        }

        public async Task<List<Models.AdminLog>> GetForTargetAsync(string targetTable, int targetId)
        {
            var logs = await _context.AdminLogs
                .Include(l => l.Admin)
                .AsNoTracking()
                .Where(l => l.TargetTable == targetTable && l.TargetId == targetId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return logs.Select(ToReportsModel).ToList();
        }

        // 篩選 + 分頁查詢,給檢舉審核台的操作紀錄分頁用(版面比照會員管理的操作紀錄分頁)。
        // 用「動作名稱」而不是 TargetTable 來界定範圍:檢舉模組寫的紀錄裡,
        // 「自動停權」「接近停權門檻」這兩種是記在 TargetTable="Members"(因為改動的是會員狀態),
        // 不是 TargetTable="Reports",用 TargetTable 篩會漏掉這兩種。
        public async Task<(List<Models.AdminLog> Data, int TotalCount)> QueryAsync(
            IEnumerable<string> actionScope, string? operatorKeyword, string? detailKeyword, string? action, int page, int pageSize = 10)
        {
            var scopeList = actionScope.ToList();
            var query = _context.AdminLogs
                .Include(l => l.Admin)
                .AsNoTracking()
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

            var result = logs.Select(ToReportsModel).ToList();

            // Description 是自由文字,資料庫端查不了關鍵字,拿到這一頁的資料後在記憶體裡篩
            // (跟 MemberService.GetMemberAdminLogs 的「被處分會員姓名」篩選是同一種取捨)
            if (!string.IsNullOrWhiteSpace(detailKeyword))
            {
                result = result.Where(r => r.Detail.Contains(detailKeyword, StringComparison.OrdinalIgnoreCase)).ToList();
                totalCount = result.Count;
            }

            return (result, totalCount);
        }

        private static Models.AdminLog ToReportsModel(AdminLog log) => new()
        {
            Id = log.LogId,
            OperatorName = log.Admin?.Name ?? "系統管理員",
            Action = log.Action,
            TargetTable = log.TargetTable,
            TargetId = log.TargetId,
            Detail = log.Description ?? string.Empty,
            IPAddress = log.Ipaddress,
            CreatedAt = log.CreatedAt
        };
    }
}
