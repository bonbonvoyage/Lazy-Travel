using LazyTravel.Models;

namespace LazyTravel.Services
{
    // TODO(後續):改用 DbContext 從 Reports 資料表查詢/更新,目前先用假資料讓畫面可運作
    public class ReportService : IReportService
    {
        private const int PageSize = 10;

        // 累犯自動停權門檻:同一帳號「檢舉成立」超過 3 次才停權,依累犯次數決定停權天數
        // (第 4 次:3 天,第 5 次以上:5 天)。只算「查證屬實」的次數,單純被檢舉但不成立的不算
        public int SuspendThreshold => 3;

        // 尚未接 Cookie 認證,判定人先用隨機代稱,不使用真實隊員姓名
        private static readonly string[] _reviewerAliases = { "審核員A", "審核員B", "審核員C", "審核員D", "審核員E" };
        private static readonly Random _random = new();

        // 保護 _reports 的讀寫,避免多人同時判定同一筆造成競態
        private static readonly object _reportsLock = new();

        // ponytail: 假資料清空,改接 DbContext 查詢真正的 Reports 資料表後這個 static List 就整個拔掉
        private static readonly List<Report> _reports = new();

        private readonly INotificationService _notificationService;
        private readonly IAdminLogService _adminLogService;
        private readonly IMemberModerationService _memberModerationService;

        public ReportService(
            INotificationService notificationService,
            IAdminLogService adminLogService,
            IMemberModerationService memberModerationService)
        {
            _notificationService = notificationService;
            _adminLogService = adminLogService;
            _memberModerationService = memberModerationService;
        }

        public string GetRandomReviewerAlias() => _reviewerAliases[_random.Next(_reviewerAliases.Length)];

        public ReportQueryResult Query(ReportQueryOptions options)
        {
            List<Report> filtered;
            int totalCount, pendingCount, upheldCount, dismissedCount;

            lock (_reportsLock)
            {
                totalCount = _reports.Count;
                pendingCount = _reports.Count(r => r.Status == ReportStatus.Pending);
                upheldCount = _reports.Count(r => r.Status == ReportStatus.Upheld);
                dismissedCount = _reports.Count(r => r.Status == ReportStatus.Dismissed);

                var reports = _reports.AsEnumerable();
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
                filtered = reports
                    .OrderBy(r => r.Status == ReportStatus.Pending ? 0 : 1)
                    .ThenByDescending(r => r.Id)
                    .ToList();
            }

            var totalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PageSize));
            var page = Math.Clamp(options.Page, 1, totalPages);
            var paged = filtered.Skip((page - 1) * PageSize).Take(PageSize).ToList();

            return new ReportQueryResult
            {
                Items = paged,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                FilteredCount = filtered.Count,
                PendingCount = pendingCount,
                UpheldCount = upheldCount,
                DismissedCount = dismissedCount
            };
        }

        public Report? GetById(int id)
        {
            lock (_reportsLock)
            {
                return _reports.FirstOrDefault(r => r.Id == id);
            }
        }

        public List<Report> GetRelatedReports(Report report)
        {
            lock (_reportsLock)
            {
                return _reports
                    .Where(r => r.TargetType == report.TargetType && r.TargetId == report.TargetId && r.Id != report.Id)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToList();
            }
        }

        public int CountUpheldForAccount(string account)
        {
            lock (_reportsLock)
            {
                return _reports.Count(r => r.ReportedMemberAccount == account && r.Status == ReportStatus.Upheld);
            }
        }

        public int? GetPendingSuspensionDays(Report report)
        {
            // +1 是把「這筆案件如果被判定成立」也算進累犯次數
            var upheldCountIfUpheld = CountUpheldForAccount(report.ReportedMemberAccount) + 1;
            return CalculateSuspendDays(upheldCountIfUpheld);
        }

        public int CountMaliciousForAccount(string reporterAccount)
        {
            lock (_reportsLock)
            {
                return _reports.Count(r => r.ReporterAccount == reporterAccount && r.IsMalicious);
            }
        }

        public int? GetPendingReporterSuspensionDays(Report report)
        {
            // +1 是把「這筆案件如果被標記惡意檢舉」也算進累犯次數
            var maliciousCountIfFlagged = CountMaliciousForAccount(report.ReporterAccount) + 1;
            return CalculateSuspendDays(maliciousCountIfFlagged);
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
            Report? report;
            int upheldCount;
            int maliciousCount;

            lock (_reportsLock)
            {
                report = _reports.FirstOrDefault(r => r.Id == id);
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

                // 累犯次數以「同一個被檢舉會員帳號」+「檢舉成立」計算,不成立的不算違規
                upheldCount = _reports.Count(r =>
                    r.ReportedMemberAccount == report.ReportedMemberAccount && r.Status == ReportStatus.Upheld);

                // 檢舉人累犯次數以「同一個檢舉人帳號」+「被標記惡意檢舉」計算
                maliciousCount = _reports.Count(r => r.ReporterAccount == report.ReporterAccount && r.IsMalicious);
            }

            var resultText = decision.ToDisplayName();

            // 判定 → 呼叫組長 Service 發通知 + 寫 Log
            await _notificationService.SendAsync(
                report.ReporterAccount,
                "檢舉處理結果通知",
                $"您於 {report.CreatedAt:yyyy/MM/dd} 檢舉的「{report.TargetTitle}」,審核結果為:{resultText}");

            await _adminLogService.WriteAsync(
                reviewerName,
                "審核檢舉",
                $"檢舉單 #{report.Id}({report.TargetType.ToDisplayName()}:{report.TargetTitle})判定為「{resultText}」",
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

                var suspendDaysIfAny = CalculateSuspendDays(upheldCount);
                if (suspendDaysIfAny.HasValue)
                {
                    var suspendDays = suspendDaysIfAny.Value;
                    var suspendReason = $"累計 {upheldCount} 次檢舉成立(超過門檻 {SuspendThreshold} 次)";

                    await _memberModerationService.SuspendAsync(report.ReportedMemberAccount, suspendDays, suspendReason);

                    await _notificationService.SendAsync(
                        report.ReportedMemberAccount,
                        "帳號停權通知",
                        $"您的帳號因{suspendReason},已停權 {suspendDays} 天。");

                    await _adminLogService.WriteAsync(
                        reviewerName,
                        "自動停權",
                        $"帳號 {report.ReportedMemberAccount} 累犯 {upheldCount} 次,停權 {suspendDays} 天(觸發自檢舉單 #{report.Id})",
                        targetTable: "Members",
                        targetId: null);

                    message += $";帳號「{report.ReportedMemberAccount}」累犯 {upheldCount} 次,已自動停權 {suspendDays} 天";
                }
            }
            else if (report.IsMalicious)
            {
                // 通知檢舉人:這次檢舉被標記為惡意檢舉,跟單純誤判/證據不足分開處理
                await _notificationService.SendAsync(
                    report.ReporterAccount,
                    "惡意檢舉警告",
                    $"您於 {report.CreatedAt:yyyy/MM/dd} 提出的檢舉經審核為惡意檢舉,請勿濫用檢舉功能。");

                var reporterSuspendDaysIfAny = CalculateSuspendDays(maliciousCount);
                if (reporterSuspendDaysIfAny.HasValue)
                {
                    var suspendDays = reporterSuspendDaysIfAny.Value;
                    var suspendReason = $"累計 {maliciousCount} 次惡意檢舉(超過門檻 {SuspendThreshold} 次)";

                    await _memberModerationService.SuspendAsync(report.ReporterAccount, suspendDays, suspendReason);

                    await _notificationService.SendAsync(
                        report.ReporterAccount,
                        "帳號停權通知",
                        $"您的帳號因{suspendReason},已停權 {suspendDays} 天。");

                    await _adminLogService.WriteAsync(
                        reviewerName,
                        "自動停權(惡意檢舉)",
                        $"帳號 {report.ReporterAccount} 惡意檢舉累犯 {maliciousCount} 次,停權 {suspendDays} 天(觸發自檢舉單 #{report.Id})",
                        targetTable: "Members",
                        targetId: null);

                    message += $";檢舉人「{report.ReporterAccount}」惡意檢舉累犯 {maliciousCount} 次,已自動停權 {suspendDays} 天";
                }
            }

            return new JudgeOutcome { Found = true, Message = message };
        }

        // 新增一筆檢舉（例如小編對會員文章提出檢舉）。跟 JudgeAsync 共用同一個 _reports 記憶體清單，
        // 不動既有的判定/查詢邏輯，純粹多一筆 Pending 狀態的紀錄進去。
        public async Task<Report> SubmitAsync(ReportTargetType targetType, int targetId, string targetTitle,
            string reportedMemberAccount, string reporterAccount, ReportReasonCategory reasonCategory, string reason)
        {
            Report newReport;
            lock (_reportsLock)
            {
                newReport = new Report
                {
                    Id = _reports.Count == 0 ? 1 : _reports.Max(r => r.Id) + 1,
                    TargetType = targetType,
                    TargetId = targetId,
                    TargetTitle = targetTitle,
                    ReportedMemberAccount = reportedMemberAccount,
                    ReporterAccount = reporterAccount,
                    ReasonCategory = reasonCategory,
                    Reason = reason,
                    Status = ReportStatus.Pending,
                    CreatedAt = DateTime.Now,
                };
                _reports.Add(newReport);
            }

            await _adminLogService.WriteAsync(
                reporterAccount,
                "提出檢舉",
                $"檢舉 {targetType.ToDisplayName()}「{targetTitle}」：{reason}",
                targetTable: "Reports",
                targetId: newReport.Id);

            return newReport;
        }

        public async Task<List<AdminLog>> GetRecentReportLogsAsync(int take)
        {
            var recentLogs = await _adminLogService.GetRecentAsync(50);
            return recentLogs.Where(l => l.TargetTable == "Reports").Take(take).ToList();
        }

        public async Task<AdminLog?> GetReviewLogAsync(int reportId)
        {
            var logs = await _adminLogService.GetForTargetAsync("Reports", reportId);
            return logs.FirstOrDefault();
        }
    }
}
