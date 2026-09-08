using LazyTravel.Shared.Models;
using LazyTravel.Shared.Models.DTOs;
using EfReport = LazyTravel.Shared.Models.EfModels.Report;
using EfMember = LazyTravel.Shared.Models.EfModels.Member;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace LazyTravel.Shared.Services
{
    public class ReportService : IReportService
    {
        private const int PageSize = 10;

        // 累犯自動停權門檻:同一帳號「檢舉成立」超過 3 次才停權,依累犯次數決定停權天數
        // (第 4 次:3 天,第 5 次以上:5 天)。只算「查證屬實」的次數,單純被檢舉但不成立的不算
        public int SuspendThreshold => 3;

        // 尚未接 Cookie 認證,還沒有真的「當前登入管理員」可以拿,判定人先從 Employees 表隨機挑一位真實員工代稱

        // 後台人員不在 Members 表,但 Reports.ReporterID 是 NOT NULL 外鍵,
        // 由後台代為提出的檢舉就掛在這個佔位帳號底下(跟 AdminLogService 用的是同一筆)
        private const string SystemAdminEmail = "system-admin@lazytravel.local";

        private readonly LazyTravel.Shared.Models.EfModels.LazyTravelDBContext _context;
        private readonly INotificationService _notificationService;
        private readonly IAdminLogService _adminLogService;
        private readonly IMemberModerationService _memberModerationService;
        private readonly IReportLookupService _lookupService;
        // 會員自己在主頁送出檢舉時可以附上截圖佐證（見 SubmitMemberReportAsync），
        // 跟期中版本 /Report/Create 用的是同一個服務、同一種「folder 分類」用法，
        // 直接用預設(非 keyed)註冊的那份就好(tours-api/admin-api 的 Program.cs 都已經注了 R2ImageStorageService)。
        private readonly IImageStorageService _imageStorage;

        public ReportService(
            LazyTravel.Shared.Models.EfModels.LazyTravelDBContext context,
            INotificationService notificationService,
            IAdminLogService adminLogService,
            IMemberModerationService memberModerationService,
            IReportLookupService lookupService,
            IImageStorageService imageStorage)
        {
            _context = context;
            _notificationService = notificationService;
            _adminLogService = adminLogService;
            _memberModerationService = memberModerationService;
            _lookupService = lookupService;
            _imageStorage = imageStorage;
        }

        // Reports 的資料庫存取層(EfModels.Report,欄位跟資料表一模一樣,byte/raw 型別)
        // 跟畫面/邏輯層(這個檔案其他地方用的 Models.Report,enum 型別、有計算屬性)故意分成兩個類別。
        // 這裡負責兩者之間的轉換,好處是資料庫層永遠跟著官方 schema 走、不會漂移,
        // 但畫面/Controller/Service 的程式碼可以繼續享有 enum 的型別安全,不用整批改成 byte。
        private static Report ToDomain(EfReport r) => new()
        {
            Id = r.ReportId,
            ReporterId = r.ReporterId,
            ReportedMemberId = r.ReportedMemberId,
            TargetType = (ReportTargetType)r.ReportType,
            TargetId = r.TargetId,
            TargetTitle = r.TargetTitle,
            ReasonCategory = (ReportReasonCategory)r.ReasonCategory,
            Reason = r.Reason,
            Description = r.Description,
            EvidenceUrl = r.EvidenceUrl,
            Status = (ReportStatus)r.ReportStatus,
            CreatedAt = r.CreatedAt,
            AdminNotes = r.AdminNotes,
            IsMalicious = r.IsMalicious,
            Reporter = r.Reporter,
            ReportedMember = r.ReportedMember
        };

        // 抓全部案件(含 Reporter/ReportedMember 關聯資料),讓 .ReporterAccount/.ReportedMemberAccount
        // 這兩個計算屬性有東西可以帶。資料量目前不大,每次直接整批撈進記憶體再用 LINQ 篩選/排序,
        // 跟原本操作記憶體假資料 List 的寫法一致,之後資料量真的變大了再改成資料庫端分頁查詢
        private List<Report> LoadReports()
        {
            return _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ReportedMember)
                .AsNoTracking()
                .ToList()
                .Select(ToDomain)
                .ToList();
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
            var efReport = _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ReportedMember)
                .AsNoTracking()
                .FirstOrDefault(r => r.ReportId == id);
            return efReport == null ? null : ToDomain(efReport);
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

        // 後台其他模組(例如 Vlog 行程文章的「提出檢舉」)直接建立檢舉單用。
        // 前台的 /Report/Create 走自己的流程(可以上傳截圖),這裡是後台內部呼叫,沒有證據圖。
        //
        // 注意:呼叫端傳進來的兩個 account 參數不一定是 Email。Vlog 後台傳的是顯示名稱
        // (被檢舉人是 Member.Name,檢舉人是登入員工的 User.Identity.Name),所以這裡 Email 跟
        // Name 都要試。而後台員工不在 Members 裡,但 Reports.ReporterID 是 NOT NULL 外鍵指向
        // Members,查不到時就退回系統管理員那筆佔位會員,真正的操作人姓名記在 AdminLogs 裡不會遺失。
        public async Task<Report> SubmitAsync(ReportTargetType targetType, int targetId, string targetTitle,
            string reportedMemberAccount, string reporterAccount, ReportReasonCategory reasonCategory, string reason, int employeeId)
        {
            var matchedReporterId = await ResolveMemberIdAsync(reporterAccount);
            var isProxySubmit = matchedReporterId is null;

            // 佔位會員不存在就自己補一筆,不要把整個送出檢舉擋掉——它是 Reports.ReporterID
            // 這個 NOT NULL 外鍵的必要佔位人,不是真人帳號,被人從 Members 清掉是會發生的事
            var reporterId = matchedReporterId ?? await EnsureSystemAdminMemberIdAsync();

            // Reports.ReportedMemberID 允許 NULL,查不到就留空,不擋下整筆檢舉
            var reportedMemberId = await ResolveMemberIdAsync(reportedMemberAccount);

            var efReport = new EfReport
            {
                ReporterId = reporterId,
                ReportedMemberId = reportedMemberId,
                ReportType = (byte)targetType,
                TargetId = targetId,
                TargetTitle = targetTitle,
                ReasonCategory = (byte)reasonCategory,
                Reason = reason,
                // 檢舉人是後台員工(不在 Members)時,把真正的操作人記進補充說明,避免只看到系統管理員
                Description = isProxySubmit ? $"由後台人員「{reporterAccount}」代為提出" : null,
                ReportStatus = (byte)ReportStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _context.Reports.Add(efReport);
            await _context.SaveChangesAsync();

            await _adminLogService.WriteAsync(
                employeeId,
                "提出檢舉",
                $"檢舉 {targetType.ToDisplayName()}「{targetTitle}」:{reason}",
                targetResource: "Reports",
                targetId: efReport.ReportId.ToString());

            return ToDomain(efReport);
        }

        // 會員自己在主頁按「檢舉」用的簡化版，見 IReportService.SubmitMemberReportAsync 的註解。
        public async Task<Report> SubmitMemberReportAsync(int reporterId, int reportedMemberId, ReportReasonCategory reasonCategory, string reason, IFormFile? evidence = null)
        {
            var reportedMember = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == reportedMemberId);

            // 截圖佐證是選填，比照期中版本 /Report/Create 的做法，folder 用 "reports" 分類，
            // 有選檔案(Length > 0)才真的上傳，避免前端沒選檔案時傳來一個空檔案物件也去打 API。
            string? evidenceUrl = null;
            if (evidence != null && evidence.Length > 0)
            {
                evidenceUrl = await _imageStorage.UploadAsync(evidence, "reports");
            }

            var efReport = new EfReport
            {
                ReporterId = reporterId,
                ReportedMemberId = reportedMemberId,
                ReportType = (byte)ReportTargetType.Member,
                // 檢舉「會員」類型時,被檢舉內容就是這個會員本人,對象編號直接沿用會員編號
                // (跟 ReportController.Create 那邊的既有規則一致)。
                TargetId = reportedMemberId,
                TargetTitle = reportedMember?.Name,
                ReasonCategory = (byte)reasonCategory,
                Reason = reason,
                EvidenceUrl = evidenceUrl,
                ReportStatus = (byte)ReportStatus.Pending,
                CreatedAt = DateTime.Now,
            };

            _context.Reports.Add(efReport);
            await _context.SaveChangesAsync();

            // 這是會員自己的動作,不是後台操作,所以不寫 AdminAuditLogs
            // (那個表是給後台審核台用的操作紀錄,見上面 SubmitAsync 那邊的用法)。

            return ToDomain(efReport);
        }

        // 取得系統管理員佔位會員的 MemberID,沒有就建一筆。
        // 跟 AdminLogService.ResolveMemberIdByEmailAsync 同一套做法(含唯一鍵撞單的處理),
        // 兩邊用的是同一筆佔位會員。
        private async Task<int> EnsureSystemAdminMemberIdAsync()
        {
            var existing = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Email == SystemAdminEmail);
            if (existing is not null)
            {
                return existing.Id;
            }

            var placeholder = new EfMember
            {
                Email = SystemAdminEmail,
                Name = "系統管理員",
                CreatedAt = DateTime.Now,
            };

            try
            {
                _context.Users.Add(placeholder);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // 同時搶著建立同一筆時,Email 唯一鍵會擋下重複插入,改成查已經存在的那筆
                _context.Entry(placeholder).State = EntityState.Detached;
                var raceWinner = await _context.Users.AsNoTracking()
                    .FirstAsync(m => m.Email == SystemAdminEmail);
                return raceWinner.Id;
            }

            return placeholder.Id;
        }

        // account 可能是 Email 也可能是 Member.Name,兩種都試
        private async Task<int?> ResolveMemberIdAsync(string? account)
        {
            if (string.IsNullOrWhiteSpace(account))
            {
                return null;
            }

            return await _context.Users
                .Where(m => m.Email == account || m.Name == account)
                .Select(m => (int?)m.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<JudgeOutcome> JudgeAsync(int id, ReportStatus decision, string? note, bool isMalicious, int employeeId)
        {
            var efReport = await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ReportedMember)
                .FirstOrDefaultAsync(r => r.ReportId == id);

            if (efReport == null)
            {
                return new JudgeOutcome { Found = false };
            }

            if ((ReportStatus)efReport.ReportStatus != ReportStatus.Pending)
            {
                // 已經被處理過,避免重複判定(例如兩個管理員同時點開同一筆)
                return new JudgeOutcome { Found = true, Message = $"檢舉單 #{efReport.ReportId} 已經被判定過,無法重複處理" };
            }

            efReport.ReportStatus = (byte)decision;
            efReport.AdminNotes = note;
            // 惡意檢舉標記只有在「不成立」時才有意義,判定成立時直接忽略這個勾選
            efReport.IsMalicious = decision == ReportStatus.Dismissed && isMalicious;

            await _context.SaveChangesAsync();

            var report = ToDomain(efReport);

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
                employeeId,
                "審核檢舉",
                $"檢舉單 #{report.Id}({_lookupService.GetTypeName(report.TargetType)}:{report.TargetTitle})判定為「{resultText}」",
                targetResource: "Reports",
                targetId: report.Id.ToString());

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
                        violationVerbPrefix: "", logAction: "自動停權", report.Id, employeeId);
                    if (suspendMessage != null)
                    {
                        message += $";{suspendMessage}";
                    }
                    else if (upheldCount == SuspendThreshold)
                    {
                        await WarnNearThresholdAsync(report.ReportedMemberAccount, upheldCount, accountRoleLabel: "帳號", report.Id, employeeId);
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
                    violationVerbPrefix: "惡意檢舉", logAction: "自動停權(惡意檢舉)", report.Id, employeeId);
                if (suspendMessage != null)
                {
                    message += $";{suspendMessage}";
                }
                else if (maliciousCount == SuspendThreshold)
                {
                    await WarnNearThresholdAsync(report.ReporterAccount, maliciousCount, accountRoleLabel: "檢舉人", report.Id, employeeId);
                }
            }

            return new JudgeOutcome { Found = true, Message = message };
        }

        // 累犯達門檻時觸發停權 + 通知 + 寫 Log,「檢舉成立」跟「惡意檢舉」兩種情境共用同一套流程,只差顯示文字
        // 沒達門檻回傳 null(不觸發任何動作);達門檻回傳一句可以直接接到判定結果訊息後面的描述文字
        private async Task<string?> ApplySuspendIfThresholdReachedAsync(
            int memberId, string account, int violationCount, string violationReasonLabel, string accountRoleLabel,
            string violationVerbPrefix, string logAction, int reportId, int employeeId)
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
                employeeId,
                logAction,
                $"帳號 {account} {violationVerbPrefix}累犯 {violationCount} 次,停權 {suspendDays} 天(觸發自檢舉單 #{reportId})",
                targetResource: "Members",
                targetId: null);

            return $"{accountRoleLabel}「{account}」{violationVerbPrefix}累犯 {violationCount} 次,已自動停權 {suspendDays} 天";
        }

        // 累犯次數剛好等於門檻(還沒超過,不會觸發停權)時,發預警通知 + 寫進真的 AdminLogs,
        // 讓會員管理那邊的「快凍結」篩選找得到「什麼時候達到這個狀態」的紀錄,不是只靠即時查詢
        private async Task WarnNearThresholdAsync(string account, int violationCount, string accountRoleLabel, int reportId, int employeeId)
        {
            await _notificationService.SendAsync(
                account,
                "停權預警通知",
                $"您目前累計 {violationCount} 次違規查證屬實,已達自動停權門檻,再一次查證屬實將會被停權,請留意平台規範。");

            await _adminLogService.WriteAsync(
                employeeId,
                "接近停權門檻",
                $"{accountRoleLabel}「{account}」累計 {violationCount} 次違規查證屬實,已達門檻(再一次將觸發自動停權,觸發自檢舉單 #{reportId})",
                targetResource: "Members",
                targetId: null);
        }

        public async Task<List<AdminLogDto>> GetRecentReportLogsAsync(int take)
        {
            var recentLogs = await _adminLogService.GetRecentAsync(50);
            return recentLogs.Where(l => l.TargetResource == "Reports").Take(take).ToList();
        }

        // 檢舉模組會寫進 AdminAuditLogs 的動作名稱清單,用來界定「操作紀錄」分頁要撈哪些紀錄
        // (Views/Reports/Index.cshtml 的篩選下拉選單也是用同一份清單,兩邊要保持一致)
        public static readonly string[] LogActionScope = { "審核檢舉", "自動停權", "自動停權(惡意檢舉)", "接近停權門檻" };

        public Task<(List<AdminLogDto> Data, int TotalCount)> GetReportLogsAsync(string? operatorKeyword, string? detailKeyword, string? action, int page)
        {
            return _adminLogService.QueryAsync(LogActionScope, operatorKeyword, detailKeyword, action, page);
        }

        public async Task<AdminLogDto?> GetReviewLogAsync(int reportId)
        {
            var logs = await _adminLogService.GetForTargetAsync("Reports", reportId.ToString());
            return logs.FirstOrDefault();
        }
    }
}
