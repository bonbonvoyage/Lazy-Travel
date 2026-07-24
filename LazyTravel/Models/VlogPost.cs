using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LazyTravel.Models;

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
// 記得跟組員說一聲、補進資料表文件，之後 EF Core Migration 要一起加這個欄位。
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

// 欄位、型別、NULL 規則對齊《Lazy Travel 旅遊平台資料表》(0722 最新版) 第 13 表 VlogPosts。
// EF Core 接上後這個 class 可直接當 Entity 用，PK 是 PostID。
public class VlogPost
{
    [Key]
    public int PostID { get; set; }

    // 發文者會員 ID。故意不加 FK 關聯到 Members——那張表是別的組員負責的模組，
    // 目前這台機器的 LazyTravelDB 也還沒有 Members 表，先讓 VlogPosts 這幾張表自己獨立可跑。
    // Members 接上後，MemberLookup 可以直接換成查真的表。
    [Required(ErrorMessage = "缺少發文會員")]
    [Display(Name = "發布會員")]
    public int MemberID { get; set; }

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

    // 資料表上的欄位叫 TravelPeople（0722 版新增的第 13 項，型別寫 nvarchar(50)）。
    // 這裡跟組員談好維持現有的 enum 下拉選單不變，只把 DB 欄位名稱對齊過去，型別改存 tinyint。
    [Display(Name = "旅遊人數")]
    [Column("TravelPeople")]
    public TravelGroupSize GroupSize { get; set; } = TravelGroupSize.Solo;

    // 規格書 0722 版已經正式收錄（原本 v1.2.1 沒有，是先前額外加的）。
    [Display(Name = "出遊日期")]
    public DateOnly? TravelDate { get; set; }

    [Display(Name = "文章狀態")]
    public VlogPostStatus Status { get; set; } = VlogPostStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    // 軟刪除：後台「刪除」動作只是把這個欄位設為 true，不是真的砍資料列。
    public bool IsDelete { get; set; }
}
