using LazyTravel.Models;
using LazyTravel.Models.EfModels;

namespace LazyTravel.Areas.Admin.Models;

public class VlogPostIndexViewModel
{
    public List<VlogPost> Posts { get; set; } = new();

    // 目前這一頁每篇文章的讚數/收藏數，Key 是 PostID。改接資料庫後改由 Controller 一次查完帶進來，
    // View 不再各自呼叫 PostInteractionStore，避免每一列各打一次資料庫。
    public Dictionary<int, int> LikeCounts { get; set; } = new();
    public Dictionary<int, int> FavoriteCounts { get; set; } = new();

    // 這一頁的會員文章裡，哪些 PostId 目前已經有待處理檢舉——已經被檢舉過的就不用再顯示「提出檢舉」
    public HashSet<int> PostIdsWithPendingReport { get; set; } = new();

    // 目前的篩選條件，換頁時要一併帶著，避免篩選被重置
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public bool ShowDeleted { get; set; }
    public string Sort { get; set; } = "updated_desc";

    // 只看官方（登入管理員）自己發布的文章，用來對應「後台只有官方能登入」這個規則
    public bool OnlyMine { get; set; }

    // 分頁
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 8;
    public int TotalCount { get; set; }
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    // 頂部統計卡（統計未刪除的文章，已刪除單獨計）
    public int TotalPosts { get; set; }
    public int PublishedCount { get; set; }
    public int DraftCount { get; set; }
    public int PendingReviewCount { get; set; }
    public int DeletedCount { get; set; }

    // 操作紀錄籤用，只取 TargetTable="VlogPosts" 的紀錄
    public List<LazyTravel.Models.AdminLog> RecentLogs { get; set; } = new();
}
