using LazyTravel.Models;

namespace LazyTravel.Areas.Admin.Models;

public class VlogPostDetailsViewModel
{
    public VlogPost Post { get; set; } = null!;
    public List<ItineraryNode> Nodes { get; set; } = new();
    public int LikeCount { get; set; }
    public int FavoriteCount { get; set; }

    // 審核資訊
    public string ReviewStatus { get; set; } = string.Empty;
    public int ReportCount { get; set; }
    public string? LastReviewer { get; set; }
    public DateTime? LastReviewedAt { get; set; }
    public string? LastReviewNote { get; set; }

    public VlogPostPermissions Permissions { get; set; } = new();
}
