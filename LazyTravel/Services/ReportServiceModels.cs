using LazyTravel.Models;

namespace LazyTravel.Services
{
    // Controller 傳給 Service 的查詢條件,對應檢舉審核台的篩選列
    public class ReportQueryOptions
    {
        public ReportTargetType? Type { get; set; }
        public ReportStatus? Status { get; set; }
        public ReportReasonCategory? ReasonCategory { get; set; }
        public string? Keyword { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
    }

    // Service 查詢完回傳給 Controller 的結果,包含分頁後的清單 + 統計數字
    public class ReportQueryResult
    {
        public List<Report> Items { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        // 套用篩選條件(Type/Status/關鍵字...)後、分頁前的筆數;TotalCount 是不管篩選的全部筆數,兩者意義不同
        public int FilteredCount { get; set; }
        public int PendingCount { get; set; }
        public int UpheldCount { get; set; }
        public int DismissedCount { get; set; }
    }

    // Judge 動作的結果,讓 Controller 決定要回什麼 HTTP 結果、TempData 要顯示什麼訊息
    public class JudgeOutcome
    {
        public bool Found { get; set; } = true;
        public string Message { get; set; } = string.Empty;
    }
}
