namespace LazyTravel.ViewModels;

public sealed class TravelGroupIndexViewModel
{
    public List<TravelGroupCardViewModel> Groups { get; init; } = new();
    public List<string> Countries { get; init; } = new();
    public List<string> AllCountries { get; init; } = new();
    public List<string> Regions { get; init; } = new();
    public Dictionary<string, List<string>> CountriesByRegion { get; init; } = new();
    public string? Country { get; init; }
    public string? Region { get; init; }
    public string? StartDate { get; init; }
    public string? EndDate { get; init; }
    public string Scope { get; init; } = "all";
    public int Page { get; init; } = 1;
    public int TotalPages { get; init; } = 1;
    public int TotalCount { get; init; }
    public int Take { get; init; } = 12;

    public string DateRangeText =>
        !string.IsNullOrWhiteSpace(StartDate) && !string.IsNullOrWhiteSpace(EndDate)
            ? $"{StartDate} - {EndDate}"
            : "";
}

public sealed class TravelGroupCardViewModel
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
    public List<string> Tags { get; init; } = new();
    public int FavoriteCount { get; init; }
    public bool IsFavorited { get; init; }
}


