using LazyTravel.Models;
using LazyTravel.Services;
using LazyTravel.Shared.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace LazyTravel.Areas.Admin.Controllers
{
	// 之後 Cookie 認證與 Role 授權建好後,改成:
	[Authorize(Policy = "RequireReportRead")]
	// Controller 只負責接請求、組 ViewModel、回傳 View,商業邏輯都在 IReportService 裡
	[Area("Admin")]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        // 從 returnUrl(清單頁的完整查詢字串)還原出當時的篩選條件,給「上一筆/下一筆」「下一筆待處理」用,
        // 這樣自動導航才會照使用者原本在看的篩選結果走,而不是整個資料庫的順序
        private static ReportQueryOptions ParseFilterFromReturnUrl(string? returnUrl)
        {
            var options = new ReportQueryOptions();
            var queryStart = returnUrl?.IndexOf('?') ?? -1;
            if (queryStart < 0)
            {
                return options;
            }

            var query = QueryHelpers.ParseQuery(returnUrl![queryStart..]);
            if (query.TryGetValue("type", out var typeVal) && Enum.TryParse<ReportTargetType>(typeVal, out var type))
            {
                options.Type = type;
            }
            if (query.TryGetValue("reasonCategory", out var reasonVal) && Enum.TryParse<ReportReasonCategory>(reasonVal, out var reason))
            {
                options.ReasonCategory = reason;
            }
            if (query.TryGetValue("keyword", out var keywordVal))
            {
                options.Keyword = keywordVal;
            }
            if (query.TryGetValue("startDate", out var startVal) && DateTime.TryParse(startVal, out var start))
            {
                options.StartDate = start;
            }
            if (query.TryGetValue("endDate", out var endVal) && DateTime.TryParse(endVal, out var end))
            {
                options.EndDate = end;
            }
            // 狀態不從 returnUrl 帶,「上一筆/下一筆」要能在同一批篩選結果裡跨狀態瀏覽,
            // 呼叫端(GetNextPending)需要強制待處理時會自己覆蓋這個欄位
            if (query.TryGetValue("status", out var statusVal) && Enum.TryParse<ReportStatus>(statusVal, out var status))
            {
                options.Status = status;
            }
            return options;
        }

        public async Task<IActionResult> Index(
            ReportTargetType? type, ReportStatus? status, ReportReasonCategory? reasonCategory,
            string? keyword, DateTime? startDate, DateTime? endDate, int page = 1,
            string? logOperator = null, string? logDetail = null, string? logAction = null, int logPage = 1)
        {
            ViewData["Title"] = "檢舉審核台";
            const int logPageSize = 10;

            // 複製貼上常會帶前後空白,先清乾淨,搜尋框回顯跟實際查詢才會一致
            keyword = keyword?.Trim();

            var result = _reportService.Query(new ReportQueryOptions
            {
                Type = type,
                Status = status,
                ReasonCategory = reasonCategory,
                Keyword = keyword,
                StartDate = startDate,
                EndDate = endDate,
                Page = page
            });

            var logResult = await _reportService.GetReportLogsAsync(logOperator, logDetail, logAction, logPage);
            var logTotalPages = (int)Math.Ceiling(logResult.TotalCount / (double)logPageSize);

            var viewModel = new ReportIndexViewModel
            {
                Items = result.Items,
                Type = type,
                Status = status,
                ReasonCategory = reasonCategory,
                Keyword = keyword,
                StartDate = startDate,
                EndDate = endDate,
                CurrentPage = result.CurrentPage,
                TotalPages = result.TotalPages,
                TotalCount = result.TotalCount,
                PendingCount = result.PendingCount,
                UpheldCount = result.UpheldCount,
                DismissedCount = result.DismissedCount,
                // 檢舉處理相關的操作紀錄,直接顯示在這頁,不用跳去別的頁面看
                RecentLogs = await _reportService.GetRecentReportLogsAsync(8),
                LogOperatorKeyword = logOperator,
                LogDetailKeyword = logDetail,
                LogAction = logAction,
                LogCurrentPage = logPage,
                LogTotalPages = logTotalPages,
                LogTotalCount = logResult.TotalCount,
                Logs = logResult.Data
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id, string? returnUrl)
        {
            var report = _reportService.GetById(id);
            if (report == null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"檢舉案件 #{report.Id}";

            // 只接受站內相對網址,避免外部惡意連結夾帶 returnUrl 造成開放重導向
            var safeReturnUrl = returnUrl != null && Url.IsLocalUrl(returnUrl) ? returnUrl : null;

            // 累犯次數各自只查一次,PendingSuspensionDays 用同一個數字往下算,避免重複查詢
            // 用 MemberID 比對(不是 Email),ReportedMemberId 理論上一定有值(見 Report.cs 的說明),沒有就當 0 次
            var upheldCountForAccount = report.ReportedMemberId.HasValue ? _reportService.CountUpheldForMember(report.ReportedMemberId.Value) : 0;
            var maliciousCountForReporter = _reportService.CountMaliciousForReporter(report.ReporterId);

            var filterOptions = ParseFilterFromReturnUrl(safeReturnUrl);
            var (previousId, nextId) = _reportService.GetAdjacentIds(report.Id, filterOptions);

            var viewModel = new ReportDetailsViewModel
            {
                Report = report,
                RelatedReports = _reportService.GetRelatedReports(report),
                ReviewLog = await _reportService.GetReviewLogAsync(report.Id),
                UpheldCountForAccount = upheldCountForAccount,
                SuspendThreshold = _reportService.SuspendThreshold,
                PendingSuspensionDays = report.Status == ReportStatus.Pending
                    ? _reportService.GetPendingSuspensionDays(upheldCountForAccount)
                    : null,
                MaliciousCountForReporter = maliciousCountForReporter,
                PendingReporterSuspensionDays = report.Status == ReportStatus.Pending
                    ? _reportService.GetPendingReporterSuspensionDays(maliciousCountForReporter)
                    : null,
                ReturnUrl = safeReturnUrl,
                PreviousId = previousId,
                NextId = nextId
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Judge(JudgeRequest request)
        {
            var safeReturnUrl = request.ReturnUrl != null && Url.IsLocalUrl(request.ReturnUrl) ? request.ReturnUrl : null;

            if (!ModelState.IsValid)
            {
                var errors = string.Join(";", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["Message"] = $"送出失敗:{errors}";
                return RedirectToAction(nameof(Details), new { id = request.Id, returnUrl = safeReturnUrl });
            }

            if (request.Decision != ReportStatus.Upheld && request.Decision != ReportStatus.Dismissed)
            {
                TempData["Message"] = "送出失敗:判定結果只能是「檢舉成立」或「不成立」";
                return RedirectToAction(nameof(Details), new { id = request.Id, returnUrl = safeReturnUrl });
            }

            // 審核人員一律取登入員工姓名(Admin/AuthController 登入時寫進 ClaimTypes.Name 的 Employees.Name),
            // 沒登入就擋下來,不編一個代稱掛名 —— 操作紀錄的重點就是「誰做的」,寫錯人比沒得寫更糟
            var reviewerName = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(reviewerName))
            {
                TempData["Message"] = "送出失敗:請先登入後台再進行審核";
                return RedirectToAction(nameof(Details), new { id = request.Id, returnUrl = safeReturnUrl });
            }

            var outcome = await _reportService.JudgeAsync(request.Id, request.Decision, request.Note, request.IsMalicious, reviewerName);

            if (!outcome.Found)
            {
                return NotFound();
            }

            TempData["Message"] = outcome.Message;

            // 判定完直接接下一筆待處理案件(同一篩選條件下),不用先跳回清單再點下一個;
            // 待處理案件都清空了才回清單,清單網址都沒有才退回這筆自己(舊行為,保底)
            var filterOptions = ParseFilterFromReturnUrl(safeReturnUrl);
            var nextPending = _reportService.GetNextPending(filterOptions);
            if (nextPending != null)
            {
                return RedirectToAction(nameof(Details), new { id = nextPending.Id, returnUrl = safeReturnUrl });
            }
            if (safeReturnUrl != null)
            {
                return Redirect(safeReturnUrl);
            }
            return RedirectToAction(nameof(Details), new { id = request.Id, returnUrl = safeReturnUrl });
        }
    }
}
