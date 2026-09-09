using LazyTravel.Models.EfModels;

namespace LazyTravel.Models;

// 前台「找行程」頁面用的 ViewModel，聚合搜尋條件、精選文章、排行榜。
public class ExploreIndexViewModel
{
    public string? Region { get; set; }
    public string? Keyword { get; set; }
    public List<string> Regions { get; set; } = new();

    // 主打的精選文章（優先選官方帳號發的，沒有的話退而求其次選最新更新的）
    public VlogPost? Featured { get; set; }

    // Featured.Content 是 HTML，這裡放去掉標籤後的純文字摘要，給首頁預覽用
    public string FeaturedExcerpt { get; set; } = string.Empty;

    // 精選文章旁邊的小卡片列
    public List<VlogPost> SideList { get; set; } = new();

    // 人氣排行榜（依讚數+收藏數排序）
    public List<VlogPost> Ranking { get; set; } = new();
}
