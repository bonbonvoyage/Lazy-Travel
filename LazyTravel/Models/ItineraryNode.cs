using System.ComponentModel.DataAnnotations;

namespace LazyTravel.Models;

// 對應規格書 (0722 最新版) 第 14 表「每日行程節點明細表 (ItineraryNodes)」
public class ItineraryNode
{
    [Key]
    public int NodeID { get; set; }

    // FK -> VlogPosts.PostID，在 LazyTravelContext.OnModelCreating 設定。
    public int PostID { get; set; }

    [Range(1, 60, ErrorMessage = "第幾天請輸入 1-60 之間")]
    [Display(Name = "第幾天")]
    public int DayNumber { get; set; } = 1;

    [Required(ErrorMessage = "請輸入景點名稱")]
    [StringLength(100, ErrorMessage = "景點名稱不可超過 100 字")]
    [Display(Name = "景點名稱")]
    public string LocationName { get; set; } = string.Empty;

    [Display(Name = "抵達時間")]
    public TimeOnly? ArrivalTime { get; set; }

    [StringLength(50)]
    [Display(Name = "停留時間")]
    public string? StayTime { get; set; }

    [Display(Name = "離開時間")]
    public TimeOnly? DepartureTime { get; set; }

    [StringLength(500)]
    [Display(Name = "專屬圖片/影片連結")]
    public string? MediaUrl { get; set; }

    [Display(Name = "媒體類型")]
    public VlogMediaType MediaType { get; set; } = VlogMediaType.Photo;

    [Display(Name = "景點介紹心得")]
    public string? Description { get; set; }

    [Display(Name = "備註")]
    public string? Remarks { get; set; }
}
