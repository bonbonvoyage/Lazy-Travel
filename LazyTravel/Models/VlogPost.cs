using System.ComponentModel.DataAnnotations;

namespace LazyTravel.Models;

// 對應規格書「模組 C - 12. 旅遊行程文章主檔 (VlogPosts)」。
// 狀態只有兩態：0=草稿, 1=發布。「下架/移除」用 IsDelete 軟刪除表示，不是另外的 Status。
public enum VlogPostStatus
{
    Draft = 0,
    Published = 1,
}

public static class VlogPostStatusExtensions
{
    public static string ToLabel(this VlogPostStatus status) => status switch
    {
        VlogPostStatus.Draft => "草稿",
        VlogPostStatus.Published => "已發布",
        _ => status.ToString(),
    };

    // 對應 admin.css 既有的 .status-pill 樣式
    public static string ToPillClass(this VlogPostStatus status) => status switch
    {
        VlogPostStatus.Draft => "status-pending",
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

// 欄位、型別、NULL 規則對齊《Lazy Travel 旅遊平台資料表》第 12 表 VlogPosts。
// EF Core 接上後這個 class 可直接當 Entity 用，PK 是 PostID。
public class VlogPost
{
    public int PostID { get; set; }

    // 發文者會員 ID。Members 資料表/後台尚未建立，先用 MemberLookup 暫時對照顯示姓名。
    [Required(ErrorMessage = "請選擇發文會員")]
    [Display(Name = "發文會員")]
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

    // 規格書 v1.2.1 原本沒有這個欄位，是額外加的：選填，記錄「實際出遊」的日期，
    // 跟 CreatedAt（發文時間）分開，因為很多人玩完才寫文章，兩者常常對不上。
    // 記得跟組員說一聲、補進資料表文件，之後 EF Core Migration 要一起加這個欄位。
    [Display(Name = "出遊日期")]
    public DateOnly? TravelDate { get; set; }

    [Display(Name = "狀態")]
    public VlogPostStatus Status { get; set; } = VlogPostStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    // 軟刪除：後台「刪除」動作只是把這個欄位設為 true，不是真的砍資料列。
    public bool IsDelete { get; set; }
}
