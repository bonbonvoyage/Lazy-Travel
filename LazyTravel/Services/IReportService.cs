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

        // 判定完一筆後,同一篩選條件下的下一筆待處理案件;沒有的話回傳 null
        Report? GetNextPending(ReportQueryOptions filterOptions);

        // 案件詳情頁「上一筆／下一筆」手動導航,回傳目前這筆在篩選條件下的前後案件編號
        (int? PreviousId, int? NextId) GetAdjacentIds(int currentId, ReportQueryOptions filterOptions);

        // 同一個會員(用 MemberID 比對,不是 Email)被查證屬實(檢舉成立)的次數
        int CountUpheldForMember(int memberId);

        // 如果這筆案件被判定「成立」,會不會觸發自動停權;不會回傳 null,會的話回傳停權天數
        // upheldCountForAccount 由呼叫端傳入(通常就是 CountUpheldForMember 的結果),避免重複查詢同一個數字
        int? GetPendingSuspensionDays(int upheldCountForAccount);

        // 同一個檢舉人(用 MemberID 比對,不是 Email)被標記「惡意檢舉」的次數
        int CountMaliciousForReporter(int reporterId);

        // 如果這筆案件被標記為惡意檢舉,檢舉人會不會被停權;不會回傳 null,會的話回傳停權天數
        int? GetPendingReporterSuspensionDays(int maliciousCountForReporter);

        // 尚未接 Cookie 認證前,判定人先用隨機代稱
        string GetRandomReviewerAlias();

        Task<JudgeOutcome> JudgeAsync(int id, ReportStatus decision, string? note, bool isMalicious, string reviewerName);

        // 這個模組相關的操作紀錄(給檢舉審核台頁面用)
        Task<List<AdminLog>> GetRecentReportLogsAsync(int take);

        // 操作紀錄篩選 + 分頁(版面比照會員管理的操作紀錄分頁)
        Task<(List<AdminLog> Data, int TotalCount)> GetReportLogsAsync(string? operatorKeyword, string? detailKeyword, string? action, int page);

        // 單一案件的審核紀錄(誰審的、什麼時候審的,取自 AdminLogs)
        Task<AdminLog?> GetReviewLogAsync(int reportId);
    }
}
