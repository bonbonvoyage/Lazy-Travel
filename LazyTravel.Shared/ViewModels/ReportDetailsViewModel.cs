using LazyTravel.Models;

namespace LazyTravel.ViewModels
{
    // 檢舉案件詳情頁的強型別 ViewModel,取代原本散落各處的 ViewData["..."]
    public class ReportDetailsViewModel
    {
        public Report Report { get; set; } = null!;
        public List<Report> RelatedReports { get; set; } = new();
        public AdminLog? ReviewLog { get; set; }
        public int UpheldCountForAccount { get; set; }
        public int SuspendThreshold { get; set; }

        // 若此案件被判定「成立」會觸發的停權天數;不會觸發則為 null
        public int? PendingSuspensionDays { get; set; }

        public int MaliciousCountForReporter { get; set; }

        // 若此案件被標記為惡意檢舉,檢舉人會觸發的停權天數;不會觸發則為 null
        public int? PendingReporterSuspensionDays { get; set; }

        // 從審核台帶進來的篩選條件(查詢字串),讓「返回列表」可以帶回去,不會清空搜尋結果
        public string? ReturnUrl { get; set; }

        // 目前篩選條件下,上一筆／下一筆案件的編號;沒有則為 null(按鈕會停用)
        public int? PreviousId { get; set; }
        public int? NextId { get; set; }
    }
}
