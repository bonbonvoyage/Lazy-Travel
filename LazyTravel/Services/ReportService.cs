using LazyTravel.Models;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Services
{
    public class ReportService : IReportService
    {
        private const int PageSize = 10;

        // 累犯自動停權門檻:同一帳號「檢舉成立」超過 3 次才停權,依累犯次數決定停權天數
        // (第 4 次:3 天,第 5 次以上:5 天)。只算「查證屬實」的次數,單純被檢舉但不成立的不算
        public int SuspendThreshold => 3;

        // 尚未接 Cookie 認證,還沒有真的「當前登入管理員」可以拿,判定人先從 Employees 表隨機挑一位真實員工代稱

        private readonly LazyTravelContext _context;
        private readonly INotificationService _notificationService;
        private readonly IAdminLogService _adminLogService;
        private readonly IMemberModerationService _memberModerationService;
        private readonly IReportLookupService _lookupService;

        public ReportService(
            LazyTravelContext context,
            INotificationService notificationService,
            IAdminLogService adminLogService,
            IMemberModerationService memberModerationService,
            IReportLookupService lookupService)
        {
            _context = context;
            _notificationService = notificationService;
            _adminLogService = adminLogService;
            _memberModerationService = memberModerationService;
            _lookupService = lookupService;
        }

        // 抓全部案件(含 Reporter/ReportedMember 關聯資料),讓 .ReporterAccount/.ReportedMemberAccount
        // 這兩個計算屬性有東西可以帶。資料量目前不大,每次直接整批撈進記憶體再用 LINQ 篩選/排序,
        // 跟原本操作記憶體假資料 List 的寫法一致,之後資料量真的變大了再改成資料庫端分頁查詢
        private List<Report> LoadReports()
        {
            return _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ReportedMember)
                .AsNoTracking()
                .ToList();
        }

        public string GetRandomReviewerAlias()
        {
            var employeeNames = _context.Employees.Select(e => e.Name).ToList();
            if (employeeNames.Count == 0)
            {
                return "審核員";
            }
            // Random.Shared 是執行緒安全的(.NET 6+);ASP.NET Core 每個請求可能跑在不同執行緒,
            // 之前用手動建立的 static Random 共用會有併發問題,內部狀態壞掉後 Next() 會一直回傳同一個值
            return employeeNames[Random.Shared.Next(employeeNames.Count)];
        }

        public ReportQueryResult Query(ReportQueryOptions options)
        {
            var allReports = LoadReports();

            var totalCount = allReports.Count;
            var pendingCount = 0;
            var upheldCount = 0;
            var dismissedCount = 0;
            foreach (var r in allReports)
            {
                switch (r.Status)
                {
                    case ReportStatus.Pending: pendingCount++; break;
                    case ReportStatus.Upheld: upheldCount++; break;
                    case ReportStatus.Dismissed: dismissedCount++; break;
                }
            }

            var filtered = ApplyFilters(allReports, options);

            var totalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PageSize));
            var page = Math.Clamp(options.Page, 1, totalPages);
            var paged = filtered.Skip((page - 1) * PageSize).Take(PageSize).ToList();

            return new ReportQueryResult
            {
                Items = paged,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PendingCount = pendingCount,
                UpheldCount = upheldCount,
                DismissedCount = dismissedCount
            };
        }

        // Query() 跟 GetAdjacentIds()/GetNextPending() 都需要同一套篩選+排序邏輯,抽出來共用,
        // 避免兩邊各自維護一份條件,篩選規則改了卻只改到一邊
        private static List<Report> ApplyFilters(IEnumerable<Report> source, ReportQueryOptions options)
        {
            var reports = source;
            if (options.Type.HasValue)
            {
                reports = reports.Where(r => r.TargetType == options.Type.Value);
            }
            if (options.Status.HasValue)
            {
                reports = reports.Where(r => r.Status == options.Status.Value);
            }
            if (options.ReasonCategory.HasValue)
            {
                reports = reports.Where(r => r.ReasonCategory == options.ReasonCategory.Value);
            }
            if (!string.IsNullOrWhiteSpace(options.Keyword))
            {
                // Keyword 已由 Controller trim 過,這裡直接用;只查檢舉人帳號、被檢舉會員帳號兩個欄位
                reports = reports.Where(r =>
                    r.ReporterAccount.Contains(options.Keyword, StringComparison.OrdinalIgnoreCase) ||
                    r.ReportedMemberAccount.Contains(options.Keyword, StringComparison.OrdinalIgnoreCase));
            }
            if (options.StartDate.HasValue)
            {
                reports = reports.Where(r => r.CreatedAt.Date >= options.StartDate.Value.Date);
            }
            if (options.EndDate.HasValue)
            {
                reports = reports.Where(r => r.CreatedAt.Date <= options.EndDate.Value.Date);
            }

            // 狀態優先:待處理排最前面,方便優先處理;同一個狀態內再依編號由新到舊排,
            // 兩層排序疊在一起,同一區塊內的編號就不會跳來跳去
            return reports
                .OrderBy(r => r.Status == ReportStatus.Pending ? 0 : 1)
                .ThenByDescending(r => r.Id)
                .ToList();
        }

        public Report? GetById(int id)
        {
            return _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ReportedMember)
                .AsNoTracking()
                .FirstOrDefault(r => r.Id == id);
        }

        public List<Report> GetRelatedReports(Report report)
        {
            return LoadReports()
                .Where(r => r.TargetType == report.TargetType && r.TargetId == report.TargetId && r.Id != report.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        // 判定完一筆之後,直接接下一筆待處理案件用的:套用跟清單頁一樣的篩選條件(狀態強制為待處理),
        // 取排序後的第一筆。因為剛判定完的那筆已經不是「待處理」了,所以不會抓到自己
        public Report? GetNextPending(ReportQueryOptions filterOptions)
        {
            var pendingOnly = new ReportQueryOptions
            {
                Type = filterOptions.Type,
                ReasonCategory = filterOptions.ReasonCategory,
                Keyword = filterOptions.Keyword,
                StartDate = filterOptions.StartDate,
                EndDate = filterOptions.EndDate,
                Status = ReportStatus.Pending
            };

            return ApplyFilters(LoadReports(), pendingOnly).FirstOrDefault();
        }

        // 案件詳情頁「上一筆／下一筆」手動導航用的:在目前篩選條件下(不強制狀態),
        // 找出目前這筆在排序後清單裡的前後兩筆編號
        public (int? PreviousId, int? NextId) GetAdjacentIds(int currentId, ReportQueryOptions filterOptions)
        {
            var ordered = ApplyFilters(LoadReports(), filterOptions);
            var index = ordered.FindIndex(r => r.Id == currentId);
            if (index == -1)
            {
                return (null, null);
            }

            int? previousId = index > 0 ? ordered[index - 1].Id : null;
            int? nextId = index < ordered.Count - 1 ? ordered[index + 1].Id : null;
            return (previousId, nextId);
        }

        public int CountUpheldForMember(int memberId)
        {
            return LoadReports().Count(r => r.ReportedMemberId == memberId && r.Status == ReportStatus.Upheld);
        }

        public int? GetPendingSuspensionDays(int upheldCountForAccount)
        {
            // +1 是把「這筆案件如果被判定成立」也算進累犯次數
            return CalculateSuspendDays(upheldCountForAccount + 1);
        }

        public int CountMaliciousForReporter(int reporterId)
        {
            return LoadReports().Count(r => r.ReporterId == reporterId && r.IsMalicious);
        }

        public int? GetPendingReporterSuspensionDays(int maliciousCountForReporter)
        {
            // +1 是把「這筆案件如果被標記惡意檢舉」也算進累犯次數
            return CalculateSuspendDays(maliciousCountForReporter + 1);
        }

        // 第 4 次違規停 3 天(輕度),第 5 次以上停 5 天(重度);未達門檻回傳 null
        private int? CalculateSuspendDays(int upheldCount)
        {
            if (upheldCount <= SuspendThreshold)
            {
                return null;
            }
            return upheldCount == SuspendThreshold + 1 ? 3 : 5;
        }

        public async Task<JudgeOutcome> JudgeAsync(int id, ReportStatus decision, string? note, bool isMalicious, string reviewerName)
        {
            var report = await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ReportedMember)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
            {
                return new JudgeOutcome { Found = false };
            }

            if (report.Status != ReportStatus.Pending)
            {
                // 已經被處理過,避免重複判定(例如兩個管理員同時點開同一筆)
                return new JudgeOutcome { Found = true, Message = $"檢舉單 #{report.Id} 已經被判定過,無法重複處理" };
            }

            report.Status = decision;
            report.AdminNotes = note;
            // 惡意檢舉標記只有在「不成立」時才有意義,判定成立時直接忽略這個勾選
            report.IsMalicious = decision == ReportStatus.Dismissed && isMalicious;

            await _context.SaveChangesAsync();

            // 存檔後重新整批撈一次,確保下面算累犯次數時抓到的是剛剛存進去的最新狀態
            var allReports = LoadReports();

            // 累犯次數以「同一個被檢舉會員(MemberID)」+「檢舉成立」計算,不成立的不算違規
            var upheldCount = allReports.Count(r => r.ReportedMemberId == report.ReportedMemberId && r.Status == ReportStatus.Upheld);

            // 檢舉人累犯次數以「同一個檢舉人(MemberID)」+「被標記惡意檢舉」計算
            var maliciousCount = allReports.Count(r => r.ReporterId == report.ReporterId && r.IsMalicious);

            var resultText = _lookupService.GetStatusName(decision);

            // 判定 → 呼叫組長 Service 發通知 + 寫 Log
            await _notificationService.SendAsync(
                report.ReporterAccount,
                "檢舉處理結果通知",
                $"您於 {report.CreatedAt:yyyy/MM/dd} 檢舉的「{report.TargetTitle}」,審核結果為:{resultText}");

            await _adminLogService.WriteAsync(
                reviewerName,
                "審核檢舉",
                $"檢舉單 #{report.Id}({_lookupService.GetTypeName(report.TargetType)}:{report.TargetTitle})判定為「{resultText}」",
                targetTable: "Reports",
                targetId: report.Id);

            var message = $"檢舉單 #{report.Id} 已判定為「{resultText}」";

            if (decision == ReportStatus.Upheld)
            {
                // 通知被檢舉的會員:此次違規查證屬實
                await _notificationService.SendAsync(
                    report.ReportedMemberAccount,
                    "違規通知",
                    $"您於 {report.CreatedAt:yyyy/MM/dd} 因「{report.Reason}」遭檢舉,經審核查證屬實,請留意平台規範。");

                // ReportedMemberId 官方schema允許NULL,但本系統送出檢舉時一定會填,這裡防呆一下避免萬一是空的就當作沒有對象可以停權
                if (report.ReportedMemberId.HasValue)
                {
                    var suspendMessage = await ApplySuspendIfThresholdReachedAsync(
                        report.ReportedMemberId.Value, report.ReportedMemberAccount, upheldCount, violationReasonLabel: "檢舉成立", accountRoleLabel: "帳號",
                        violationVerbPrefix: "", logAction: "自動停權", report.Id, reviewerName);
                    if (suspendMessage != null)
                    {
                        message += $";{suspendMessage}";
                    }
                    else if (upheldCount == SuspendThreshold)
                    {
                        await WarnNearThresholdAsync(report.ReportedMemberAccount, upheldCount, accountRoleLabel: "帳號", report.Id, reviewerName);
                    }
                }
            }
            else if (report.IsMalicious)
            {
                // 通知檢舉人:這次檢舉被標記為惡意檢舉,跟單純誤判/證據不足分開處理
                await _notificationService.SendAsync(
                    report.ReporterAccount,
                    "惡意檢舉警告",
                    $"您於 {report.CreatedAt:yyyy/MM/dd} 提出的檢舉經審核為惡意檢舉,請勿濫用檢舉功能。");

                var suspendMessage = await ApplySuspendIfThresholdReachedAsync(
                    report.ReporterId, report.ReporterAccount, maliciousCount, violationReasonLabel: "惡意檢舉", accountRoleLabel: "檢舉人",
                    violationVerbPrefix: "惡意檢舉", logAction: "自動停權(惡意檢舉)", report.Id, reviewerName);
                if (suspendMessage != null)
                {
                    message += $";{suspendMessage}";
                }
                else if (maliciousCount == SuspendThreshold)
                {
                    await WarnNearThresholdAsync(report.ReporterAccount, maliciousCount, accountRoleLabel: "檢舉人", report.Id, reviewerName);
                }
            }

            return new JudgeOutcome { Found = true, Message = message };
        }

        // 累犯達門檻時觸發停權 + 通知 + 寫 Log,「檢舉成立」跟「惡意檢舉」兩種情境共用同一套流程,只差顯示文字
        // 沒達門檻回傳 null(不觸發任何動作);達門檻回傳一句可以直接接到判定結果訊息後面的描述文字
        private async Task<string?> ApplySuspendIfThresholdReachedAsync(
            int memberId, string account, int violationCount, string violationReasonLabel, string accountRoleLabel,
            string violationVerbPrefix, string logAction, int reportId, string reviewerName)
        {
            var suspendDaysIfAny = CalculateSuspendDays(violationCount);
            if (!suspendDaysIfAny.HasValue)
            {
                return null;
            }

            var suspendDays = suspendDaysIfAny.Value;
            var suspendReason = $"累計 {violationCount} 次{violationReasonLabel}(超過門檻 {SuspendThreshold} 次)";

            // 用 MemberID 直接停權,不再靠 Email 反查會員(帳號只用來顯示訊息)
            await _memberModerationService.SuspendAsync(memberId, suspendDays, suspendReason);

            await _notificationService.SendAsync(
                account,
                "帳號停權通知",
                $"您的帳號因{suspendReason},已停權 {suspendDays} 天。");

            await _adminLogService.WriteAsync(
                reviewerName,
                logAction,
                $"帳號 {account} {violationVerbPrefix}累犯 {violationCount} 次,停權 {suspendDays} 天(觸發自檢舉單 #{reportId})",
                targetTable: "Members",
                targetId: null);

            return $"{accountRoleLabel}「{account}」{violationVerbPrefix}累犯 {violationCount} 次,已自動停權 {suspendDays} 天";
        }

        // 累犯次數剛好等於門檻(還沒超過,不會觸發停權)時,發預警通知 + 寫進真的 AdminLogs,
        // 讓會員管理那邊的「快凍結」篩選找得到「什麼時候達到這個狀態」的紀錄,不是只靠即時查詢
        private async Task WarnNearThresholdAsync(string account, int violationCount, string accountRoleLabel, int reportId, string reviewerName)
        {
            await _notificationService.SendAsync(
                account,
                "停權預警通知",
                $"您目前累計 {violationCount} 次違規查證屬實,已達自動停權門檻,再一次查證屬實將會被停權,請留意平台規範。");

            await _adminLogService.WriteAsync(
                reviewerName,
                "接近停權門檻",
                $"{accountRoleLabel}「{account}」累計 {violationCount} 次違規查證屬實,已達門檻(再一次將觸發自動停權,觸發自檢舉單 #{reportId})",
                targetTable: "Members",
                targetId: null);
        }

        public async Task<List<AdminLog>> GetRecentReportLogsAsync(int take)
        {
            var recentLogs = await _adminLogService.GetRecentAsync(50);
            return recentLogs.Where(l => l.TargetTable == "Reports").Take(take).ToList();
        }

        // 檢舉模組會寫進 AdminLogs 的動作名稱清單,用來界定「操作紀錄」分頁要撈哪些紀錄
        // (Views/Reports/Index.cshtml 的篩選下拉選單也是用同一份清單,兩邊要保持一致)
        public static readonly string[] LogActionScope = { "審核檢舉", "自動停權", "自動停權(惡意檢舉)", "接近停權門檻" };

        public Task<(List<AdminLog> Data, int TotalCount)> GetReportLogsAsync(string? operatorKeyword, string? detailKeyword, string? action, int page)
        {
            return _adminLogService.QueryAsync(LogActionScope, operatorKeyword, detailKeyword, action, page);
        }

        public async Task<AdminLog?> GetReviewLogAsync(int reportId)
        {
            var logs = await _adminLogService.GetForTargetAsync("Reports", reportId);
            return logs.FirstOrDefault();
        }
    }
}
