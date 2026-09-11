namespace LazyTravel.Shared.ViewModels;

// 首頁「熱門行程 / 熱門文章」兩個橫向卡片區用的資料，全部來自真實資料庫查詢，
// HomeController.Data() 直接 Ok(vm) 回傳，前端 wwwroot/js/home.js 用 fetch 取得後渲染。
public class HomeIndexViewModel
{
    public List<HomeTripCardVm> PopularTrips { get; set; } = new();
    public List<HomeArticleCardVm> PopularArticles { get; set; } = new();
}

public class HomeTripCardVm
{
    public int GroupId { get; set; }
    public string Country { get; set; } = "";
    public string GroupTitle { get; set; } = "";
    public string CoverImageUrl { get; set; } = "";
    public string DaysText { get; set; } = "";
    public int CurrentPeople { get; set; }
    public int MaxPeople { get; set; }
}

public class HomeArticleCardVm
{
    public int PostId { get; set; }
    public string Title { get; set; } = "";
    public string MediaUrl { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public string DateText { get; set; } = "";
    public int LikeCount { get; set; }
}
