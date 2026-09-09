using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LazyTravel.Shared.Models
{
    // 數值對齊《Lazy Travel 旅遊平台 - 全模組資料庫規格書》官方 ReportType 定義(1:會員,2:Vlog文章,4:揪團)
    // 官方還有 3:論壇貼文、5:留言,目前系統還沒有對應模組,先不加,等那兩個功能做出來再補
    // 底層型別指定 byte,對應資料庫的 tinyint 欄位(enum 預設底層型別是 int,跟 tinyint 讀取時型別對不上)
    public enum ReportTargetType : byte
    {
        Member = 1,
        VlogPost = 2,
        TravelGroup = 4
    }

    // 數值對齊官方 ReportStatus 定義(0:待處理,1:已處分,2:退回)
    public enum ReportStatus : byte
    {
        Pending = 0,
        Upheld = 1,
        Dismissed = 2
    }

    // 常見檢舉類別,用篩選籤讓操作人員可以直接點選,不用自己打關鍵字找
    // 這個分類不在官方 Reports schema 裡,是本機資料庫用 ALTER TABLE 額外加的欄位(2026-07-21),
    // 只存在於這個分支的本機開發環境,還沒跟團隊/DBA 提案正式收錄進共用資料庫
    public enum ReportReasonCategory : byte
    {
        Spam,           // 廣告 / 垃圾訊息
        Fraud,          // 詐騙 / 安全疑慮
        Harassment,     // 騷擾 / 不當言論
        Copyright,      // 版權 / 抄襲爭議
        ServiceDispute, // 服務 / 行程糾紛
        Other           // 其他
    }

    // 類型/類別/狀態的中文顯示文字,主要來源是資料庫,見 IReportLookupService。
    // 下面兩個 ToDisplayName 是給拿不到 IReportLookupService 的地方用的後備值
    // (例如 Vlog 行程文章的 View、以及後台內部呼叫 SubmitAsync 時組操作紀錄文字)。
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

    public static class ReportStatusExtensions
    {
        // 對應 admin.css 既有的 .status-pill 樣式(status-ok / status-pending / status-risk),
        // 這是畫面顏色的樣式代號,不是資料內容,所以繼續留在程式碼裡。
        public static string ToPillClass(this ReportStatus status) => status switch
        {
            ReportStatus.Upheld => "status-risk",
            ReportStatus.Dismissed => "status-ok",
            _ => "status-pending"
        };
    }

    // 對應資料庫實體 Reports 表(官方 9 欄 + 本機額外 ALTER TABLE 加的 ReasonCategory/IsMalicious/TargetTitle/Description/EvidenceUrl)
    [Table("Reports")]
    public class Report
    {
        [Key]
        [Column("ReportID")]
        public int Id { get; set; }

        [Column("ReporterID")]
        public int ReporterId { get; set; }

        // 官方設計:被檢舉文章/揪團時可為 NULL;本系統為了累犯停權邏輯,固定會填實際負責的會員 ID
        [Column("ReportedMemberID")]
        public int? ReportedMemberId { get; set; }

        [Column("ReportType")]
        [Display(Name = "檢舉類型")]
        public ReportTargetType TargetType { get; set; }

        [Column("TargetID")]
        [Display(Name = "被檢舉對象 Id")]
        public int? TargetId { get; set; }

        // 本機額外欄位(官方表沒有):被檢舉對象名稱快取,避免每次都要 join VlogPosts/TravelGroups(這兩張表目前也還沒有真實內容)
        [StringLength(200)]
        [Display(Name = "被檢舉對象名稱")]
        public string? TargetTitle { get; set; }

        [Display(Name = "檢舉類別")]
        public ReportReasonCategory ReasonCategory { get; set; } = ReportReasonCategory.Other;

        // 官方 Reports.Reason 是 nvarchar(500),這裡收緊到 200 字(業務規則,資料庫欄位本身不用改)
        [Required(ErrorMessage = "請輸入檢舉原因")]
        [StringLength(200, ErrorMessage = "檢舉原因不可超過 200 字")]
        [Display(Name = "檢舉原因")]
        public string Reason { get; set; } = string.Empty;

        // 本機額外欄位(官方表沒有):補充說明
        [Display(Name = "補充說明")]
        public string? Description { get; set; }

        // 本機額外欄位(官方表沒有):檢舉人從前台上傳的截圖路徑,選填
        [Display(Name = "檢舉截圖")]
        public string? EvidenceUrl { get; set; }

        [Column("ReportStatus")]
        [Display(Name = "狀態")]
        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        [Display(Name = "檢舉時間")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 審核人員/審核時間依官方 schema 不存在 Reports 表上,改成從 AdminAuditLogs 查
        // (TargetResource="Reports", TargetId=此筆 Id)反查,不再存於這個 Model

        // 官方 Reports.AdminNotes 是 nvarchar(500),這裡收緊到 200 字(業務規則,資料庫欄位本身不用改)
        [StringLength(200, ErrorMessage = "處置備註不可超過 200 字")]
        [Display(Name = "處置備註")]
        public string? AdminNotes { get; set; }

        // 只有判定「不成立」時才有意義:審核人員額外標記這是惡意檢舉(非單純誤判/證據不足),
        // 累犯達門檻會比照被檢舉方的方式停權,同一套 CalculateSuspendDays 規則
        [Display(Name = "惡意檢舉標記")]
        public bool IsMalicious { get; set; }

        // 型別指到 EfModels.Member(不是這個檔案同一個命名空間下手寫的 Member),因為這兩個屬性
        // 是 ReportService 從 LazyTravelDBContext 查出來的 EfModels.Report 轉過來的,資料來源本來就是那邊
        [ForeignKey("ReporterId")]
        public virtual EfModels.Member? Reporter { get; set; }

        [ForeignKey("ReportedMemberId")]
        public virtual EfModels.Member? ReportedMember { get; set; }

        // 畫面/篩選邏輯原本是直接用帳號字串比對,改接資料庫後這兩個計算屬性從關聯的 Member 帶出對應帳號(Email),
        // 讓 ReportService.cs、Views 裡原本寫好的 .ReporterAccount/.ReportedMemberAccount 幾乎不用改
        [NotMapped]
        public string ReporterAccount => Reporter?.Email ?? "(帳號未知)";

        [NotMapped]
        public string ReportedMemberAccount => ReportedMember?.Email ?? TargetTitle ?? "(無對應會員)";
    }
}
