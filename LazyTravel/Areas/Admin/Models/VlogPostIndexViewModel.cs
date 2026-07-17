using LazyTravel.Models;

namespace LazyTravel.Areas.Admin.Models;

public class VlogPostIndexViewModel
{
    public List<VlogPost> Posts { get; set; } = new();

    // 目前的篩選條件，換頁時要一併帶著，避免篩選被重置
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public bool ShowDeleted { get; set; }
    public string Sort { get; set; } = "updated_desc";

    // 分頁
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 8;
    public int TotalCount { get; set; }
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    // 頂部統計卡（統計未刪除的文章，已刪除單獨計）
    public int TotalPosts { get; set; }
    public int PublishedCount { get; set; }
    public int DraftCount { get; set; }
    public int DeletedCount { get; set; }
}
