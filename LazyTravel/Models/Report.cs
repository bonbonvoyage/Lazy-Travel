using System.ComponentModel.DataAnnotations;

namespace LazyTravel.Models
{
    public enum ReportTargetType
    {
        Member,
        VlogPost,
        TravelGroup
    }

    public enum ReportStatus
    {
        Pending,
        Upheld,
        Dismissed
    }

    // 常見檢舉類別,用篩選籤讓操作人員可以直接點選,不用自己打關鍵字找
    // 注意:這個分類在《Lazy Travel 旅遊平台資料表.docx》官方 Reports schema 裡還沒有對應欄位,
    // 是待跟團隊/DBA 提案新增的欄位,不是目前已核准的正式設計,之後接資料庫前要先確認要不要真的加這欄
    public enum ReportReasonCategory
    {
        Spam,           // 廣告 / 垃圾訊息
        Fraud,          // 詐騙 / 安全疑慮
        Harassment,     // 騷擾 / 不當言論
        Copyright,      // 版權 / 抄襲爭議
        ServiceDispute, // 服務 / 行程糾紛
        Other           // 其他
    }

    public static class ReportTargetTypeExtensions
    {
        public static string ToDisplayName(this ReportTargetType type) => type switch
        {
            ReportTargetType.Member => "會員",
            ReportTargetType.VlogPost => "Vlog 行程文章",
            ReportTargetType.TravelGroup => "揪團管理",
            _ => type.ToString()
        };
    }

    public static class ReportStatusExtensions
    {
        public static string ToDisplayName(this ReportStatus status) => status switch
        {
            ReportStatus.Pending => "待處理",
            ReportStatus.Upheld => "檢舉成立",
            ReportStatus.Dismissed => "不成立",
            _ => status.ToString()
        };

        // 對應 admin.css 既有的 .status-pill 樣式(status-ok / status-pending / status-risk)
        public static string ToPillClass(this ReportStatus status) => status switch
        {
            ReportStatus.Upheld => "status-risk",
            ReportStatus.Dismissed => "status-ok",
            _ => "status-pending"
        };
    }

    public static class ReportReasonCategoryExtensions
    {
        public static string ToDisplayName(this ReportReasonCategory category) => category switch
        {
            ReportReasonCategory.Spam => "廣告垃圾訊息",
            ReportReasonCategory.Fraud => "詐騙/安全疑慮",
            ReportReasonCategory.Harassment => "騷擾/不當言論",
            ReportReasonCategory.Copyright => "版權/抄襲爭議",
            ReportReasonCategory.ServiceDispute => "服務/行程糾紛",
            ReportReasonCategory.Other => "其他",
            _ => category.ToString()
        };
    }

    public class Report
    {
        public int Id { get; set; }

        [Display(Name = "檢舉類型")]
        public ReportTargetType TargetType { get; set; }

        [Display(Name = "被檢舉對象 Id")]
        public int TargetId { get; set; }

        [Required]
        [Display(Name = "被檢舉對象名稱")]
        public string TargetTitle { get; set; } = string.Empty;

        // 對應官方 Reports.ReportedMemberID:不管檢舉類型是什麼,都要能追出「究責到哪個會員帳號」
        // (檢舉會員時就是本人;檢舉 Vlog 文章/揪團時,是該內容的發文者/團主帳號)
        [Required]
        [Display(Name = "被檢舉會員帳號")]
        public string ReportedMemberAccount { get; set; } = string.Empty;

        // 顯示檢舉人的帳號(對應官方 Members.Email/帳號),不是暱稱
        [Required(ErrorMessage = "請輸入檢舉人帳號")]
        [Display(Name = "檢舉人帳號")]
        public string ReporterAccount { get; set; } = string.Empty;

        [Display(Name = "檢舉類別")]
        public ReportReasonCategory ReasonCategory { get; set; } = ReportReasonCategory.Other;

        // 官方 Reports.Reason 是 nvarchar(500),這裡收緊到 200 字(業務規則,資料庫欄位本身不用改)
        [Required(ErrorMessage = "請輸入檢舉原因")]
        [StringLength(200, ErrorMessage = "檢舉原因不可超過 200 字")]
        [Display(Name = "檢舉原因")]
        public string Reason { get; set; } = string.Empty;

        [Display(Name = "補充說明")]
        public string? Description { get; set; }

        [Display(Name = "狀態")]
        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        [Display(Name = "檢舉時間")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 審核人員/審核時間依官方 schema 不存在 Reports 表上,改成從 AdminLogs 查
        // (TargetTable="Reports", TargetID=此筆 Id)反查,不再存於這個 Model

        // 官方 Reports.AdminNotes 是 nvarchar(500),這裡收緊到 200 字(業務規則,資料庫欄位本身不用改)
        [StringLength(200, ErrorMessage = "處置備註不可超過 200 字")]
        [Display(Name = "處置備註")]
        public string? AdminNotes { get; set; }

        // 只有判定「不成立」時才有意義:審核人員額外標記這是惡意檢舉(非單純誤判/證據不足),
        // 累犯達門檻會比照被檢舉方的方式停權,同一套 CalculateSuspendDays 規則
        [Display(Name = "惡意檢舉標記")]
        public bool IsMalicious { get; set; }
    }
}
