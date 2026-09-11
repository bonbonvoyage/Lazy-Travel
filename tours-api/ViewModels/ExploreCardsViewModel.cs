using LazyTravel.Shared.Models.EfModels;

namespace LazyTravel.ViewModels;

public class ExploreCardsViewModel
{
    public List<VlogPost> Articles { get; set; } = new();
    public List<string> AllCountries { get; set; } = new();
    public HashSet<int> Favorites { get; set; } = new();
    public HashSet<int> Likes { get; set; } = new();
    public Dictionary<int, int> FavoriteCounts { get; set; } = new();
    public Dictionary<int, int> LikeCounts { get; set; } = new();
    public string? Country { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string DateRangeText => string.Join(" 至 ", new[] { StartDate, EndDate }.Where(s => !string.IsNullOrEmpty(s)));
    public string Scope { get; set; } = "all";
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public int Take { get; set; } = 12;
}


