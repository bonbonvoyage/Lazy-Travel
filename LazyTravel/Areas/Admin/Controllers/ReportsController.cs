using LazyTravel.Models;
using LazyTravel.Services;
using LazyTravel.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LazyTravel.Areas.Admin.Controllers
{
    // 之後 Cookie 認證與 Role 授權建好後,改成:
    // [Authorize(Roles = "Admin,SuperAdmin")]
    // Controller 只負責接請求、組 ViewModel、回傳 View,商業邏輯都在 IReportService 裡
    [Area("Admin")]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        public async Task<IActionResult> Index(
            ReportTargetType? type, ReportStatus? status, ReportReasonCategory? reasonCategory,
            string? keyword, DateTime? startDate, DateTime? endDate, int page = 1)
        {
            ViewData["Title"] = "檢舉審核台";

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
                RecentLogs = await _reportService.GetRecentReportLogsAsync(8)
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var report = _reportService.GetById(id);
            if (report == null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"檢舉案件 #{report.Id}";

            var viewModel = new ReportDetailsViewModel
            {
                Report = report,
                RelatedReports = _reportService.GetRelatedReports(report),
                ReviewLog = await _reportService.GetReviewLogAsync(report.Id),
                UpheldCountForAccount = _reportService.CountUpheldForAccount(report.ReportedMemberAccount),
                SuspendThreshold = _reportService.SuspendThreshold,
                PendingSuspensionDays = report.Status == ReportStatus.Pending
                    ? _reportService.GetPendingSuspensionDays(report)
                    : null,
                MaliciousCountForReporter = _reportService.CountMaliciousForAccount(report.ReporterAccount),
                PendingReporterSuspensionDays = report.Status == ReportStatus.Pending
                    ? _reportService.GetPendingReporterSuspensionDays(report)
                    : null
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Judge(JudgeRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(";", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["Message"] = $"送出失敗:{errors}";
                return RedirectToAction(nameof(Details), new { id = request.Id });
            }

            if (request.Decision != ReportStatus.Upheld && request.Decision != ReportStatus.Dismissed)
            {
                TempData["Message"] = "送出失敗:判定結果只能是「檢舉成立」或「不成立」";
                return RedirectToAction(nameof(Details), new { id = request.Id });
            }

            var reviewerName = User.Identity?.Name ?? _reportService.GetRandomReviewerAlias();
            var outcome = await _reportService.JudgeAsync(request.Id, request.Decision, request.Note, request.IsMalicious, reviewerName);

            if (!outcome.Found)
            {
                return NotFound();
            }

            TempData["Message"] = outcome.Message;
            return RedirectToAction(nameof(Details), new { id = request.Id });
        }
    }
}
