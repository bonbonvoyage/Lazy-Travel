using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LazyTravel.Models.EfModels;

// 對應規格書「模組 C - 12. 旅遊行程文章主檔 (VlogPosts)」。
// Status 只管「發布/審核」狀態：0=草稿, 1=待審核, 2=已發布。
// 「下架/移除」不算在 Status 裡，一律用 IsDelete 軟刪除表示，兩者互相獨立判斷。
public enum VlogPostStatus
{
    Draft = 0,
    PendingReview = 1,
    Published = 2,
}

public static class VlogPostStatusExtensions
{
    public static string ToLabel(this VlogPostStatus status) => status switch
    {
        VlogPostStatus.Draft => "草稿",
        VlogPostStatus.PendingReview => "待審核",
        VlogPostStatus.Published => "已發布",
        _ => status.ToString(),
    };

    // 對應 admin.css 既有的 .status-pill 樣式（沒有另外加新樣式：草稿沿用中性的 sand，
    // 待審核借用原本表示風險/需要處理的 coral，已發布維持綠色）
    public static string ToPillClass(this VlogPostStatus status) => status switch
    {
        VlogPostStatus.Draft => "status-pending",
        VlogPostStatus.PendingReview => "status-risk",
        VlogPostStatus.Published => "status-ok",
        _ => "status-pending",
    };
}

// 對應規格書的 MediaType：0=圖片, 1=影片（VlogPosts 與 ItineraryNodes 共用）
public enum VlogMediaType
{
    Photo = 0,
    Video = 1,
}

public static class VlogMediaTypeExtensions
{
    public static string ToLabel(this VlogMediaType type) => type switch
    {
        VlogMediaType.Photo => "圖片",
        VlogMediaType.Video => "影片",
        _ => type.ToString(),
    };
}

// 規格書 v1.2.1 原本沒有這個欄位，是額外加的：記錄行程適合的旅遊人數區間。
public enum TravelGroupSize
{
    Solo = 0,
    Small = 1,
    Large = 2,
}

public static class TravelGroupSizeExtensions
{
    public static string ToLabel(this TravelGroupSize size) => size switch
    {
        TravelGroupSize.Solo => "1人・獨旅",
        TravelGroupSize.Small => "2-4人・精緻團",
        TravelGroupSize.Large => "5人以上・團體",
        _ => size.ToString(),
    };
}

// 這個檔案原本是 EF Core Power Tools 反向工程產生的（PostId/MemberId 命名、Member/ItineraryNodes/
// PostInteractions 導覽屬性都是工具產生的樣子），手動把 Status/MediaType/TravelPeople 改成我們自己的
// enum，資料庫還是存 tinyint，轉換設定在 LazyTravelDBContext.OnModelCreating 用 HasConversion<byte>。
// 之後如果重新對資料庫跑一次反向工程，記得把這三個屬性的 enum 型別留著、不要被覆蓋回 byte。
public partial class VlogPost
{
    [Key]
    [Column("PostID")]
    public int PostId { get; set; }

    // 發文者會員 ID。是不是「官方」帳號用 MemberLookup.IsOfficial 判斷。
    [Column("MemberID")]
    [Required(ErrorMessage = "缺少發文會員")]
    [Display(Name = "發布會員")]
    public int MemberId { get; set; }

    [Required(ErrorMessage = "請輸入標題")]
    [StringLength(150, ErrorMessage = "標題不可超過 150 字")]
    [Display(Name = "文章大標題")]
    public string Title { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "連結不可超過 500 字")]
    [Display(Name = "封面圖片/影片連結")]
    public string? MediaUrl { get; set; }

    [Display(Name = "媒體類型")]
    public VlogMediaType MediaType { get; set; } = VlogMediaType.Photo;

    // 後台編輯器（Quill）存的是 HTML。目前只有管理員會寫入，風險較低；
    // 之後如果开放一般會員自己發文，記得在存檔前用 HtmlSanitizer 之類的套件清洗過，避免 XSS。
    [Display(Name = "行程總體心得")]
    public string? Content { get; set; }

    [Required(ErrorMessage = "請輸入目的地")]
    [StringLength(100, ErrorMessage = "目的地不可超過 100 字")]
    [Display(Name = "目的地")]
    public string Destination { get; set; } = string.Empty;

    [Range(1, 365, ErrorMessage = "總天數請輸入 1-365 之間")]
    [Display(Name = "總天數")]
    public int TravelDays { get; set; } = 1;

    // 資料庫欄位本來就叫 TravelPeople，這裡屬性名稱直接對齊，不用再額外 [Column] 重新對應。
    [Display(Name = "旅遊人數")]
    public TravelGroupSize TravelPeople { get; set; } = TravelGroupSize.Solo;

    [Column(TypeName = "datetime")]
    [Display(Name = "出遊日期")]
    public DateTime? TravelDate { get; set; }

    [Display(Name = "文章狀態")]
    public VlogPostStatus Status { get; set; } = VlogPostStatus.Draft;

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    // 軟刪除：後台「刪除」動作只是把這個欄位設為 true，不是真的砍資料列。
    public bool IsDelete { get; set; }

    [InverseProperty("Post")]
    public virtual ICollection<ItineraryNode> ItineraryNodes { get; set; } = new List<ItineraryNode>();

    [ForeignKey("MemberId")]
    [InverseProperty("VlogPosts")]
    public virtual Member? Member { get; set; }

    [InverseProperty("Post")]
    public virtual ICollection<PostInteraction> PostInteractions { get; set; } = new List<PostInteraction>();
}
