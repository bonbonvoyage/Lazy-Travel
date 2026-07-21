using LazyTravel.Models;

namespace LazyTravel.Services
{
    // 檢舉中心的商業邏輯統一收在這裡,ReportsController 只負責接請求、組 ViewModel、回傳 View
    public interface IReportService
    {
        int SuspendThreshold { get; }

        ReportQueryResult Query(ReportQueryOptions options);

        Report? GetById(int id);

        // 同一個被檢舉對象的其他檢舉紀錄(判斷是不是常被檢舉的目標)
        List<Report> GetRelatedReports(Report report);

        // 同一個會員帳號被查證屬實(檢舉成立)的次數
        int CountUpheldForAccount(string account);

        // 如果這筆案件被判定「成立」,會不會觸發自動停權;不會回傳 null,會的話回傳停權天數
        int? GetPendingSuspensionDays(Report report);

        // 同一個檢舉人帳號被標記「惡意檢舉」的次數
        int CountMaliciousForAccount(string reporterAccount);

        // 如果這筆案件被標記為惡意檢舉,檢舉人會不會被停權;不會回傳 null,會的話回傳停權天數
        int? GetPendingReporterSuspensionDays(Report report);

        // 尚未接 Cookie 認證前,判定人先用隨機代稱
        string GetRandomReviewerAlias();

        Task<JudgeOutcome> JudgeAsync(int id, ReportStatus decision, string? note, bool isMalicious, string reviewerName);

        // 這個模組相關的操作紀錄(給檢舉審核台頁面用)
        Task<List<AdminLog>> GetRecentReportLogsAsync(int take);

        // 單一案件的審核紀錄(誰審的、什麼時候審的,取自 AdminLogs)
        Task<AdminLog?> GetReviewLogAsync(int reportId);
    }
}
