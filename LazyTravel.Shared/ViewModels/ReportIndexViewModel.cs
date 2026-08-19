using LazyTravel.Models;

namespace LazyTravel.ViewModels
{
    // 檢舉審核台列表頁的強型別 ViewModel,取代原本散落各處的 ViewData["..."]
    public class ReportIndexViewModel
    {
        public List<Report> Items { get; set; } = new();

        // 篩選條件:給表單回填目前選取值、也給分頁連結帶參數用
        public ReportTargetType? Type { get; set; }
        public ReportStatus? Status { get; set; }
        public ReportReasonCategory? ReasonCategory { get; set; }
        public string? Keyword { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int UpheldCount { get; set; }
        public int DismissedCount { get; set; }

        public List<AdminLog> RecentLogs { get; set; } = new();

        // 操作紀錄分頁的篩選條件跟分頁資訊(版面比照會員管理的操作紀錄分頁)
        public string? LogOperatorKeyword { get; set; }
        public string? LogDetailKeyword { get; set; }
        public string? LogAction { get; set; }
        public int LogCurrentPage { get; set; } = 1;
        public int LogTotalPages { get; set; }
        public int LogTotalCount { get; set; }
        public List<AdminLog> Logs { get; set; } = new();

        public bool HasFilter =>
            Type != null || Status != null || ReasonCategory != null ||
            !string.IsNullOrWhiteSpace(Keyword) || StartDate != null || EndDate != null;
    }
}
