namespace LazyTravel.ViewModels;

public sealed class FavoriteTripsViewModel
{
    public List<FavoriteTripCardViewModel> Trips { get; init; } = new();
    public List<FavoriteArticleCardViewModel> Articles { get; init; } = new();
    public List<string> AllCountries { get; init; } = new();
    public string Sort { get; init; } = "newest";
    public string Type { get; init; } = "trips";
    public string? Keyword { get; init; }
    public string? Country { get; init; }
    public int ActiveCount => Type == "articles" ? Articles.Count : Trips.Count;
}

public sealed class FavoriteTripCardViewModel
{
    public int GroupId { get; init; }
    public string Title { get; init; } = "";
    public string Country { get; init; } = "";
    public string Region { get; init; } = "";
    public string DateText { get; init; } = "日期未定";
    public string CoverImageUrl { get; init; } = "";
    public int CurrentPeople { get; init; }
    public int MaxPeople { get; init; }
    public string OwnerName { get; init; } = "旅人";
    public string? OwnerAvatarUrl { get; init; }
    public byte Status { get; init; }
    public string StatusText { get; init; } = "等待中";
}

public sealed class FavoriteArticleCardViewModel
{
    public int PostId { get; init; }
    public string Title { get; init; } = "";
    public string Country { get; init; } = "";
    public string Region { get; init; } = "";
    public string DateText { get; init; } = "日期未定";
    public string PeopleText { get; init; } = "人數未定";
    public string CoverImageUrl { get; init; } = "";
    public string AuthorName { get; init; } = "旅人";
    public string? AuthorAvatarUrl { get; init; }
}
