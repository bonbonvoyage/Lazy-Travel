using LazyTravel.Shared.Models.EfModels;
namespace LazyTravel.ViewModels;
public class ExploreDetailsViewModel
{
    public VlogPost Post { get; set; } = null!;
    public VlogPostRoomExport? Source { get; set; }
    public List<ItineraryNode> Nodes { get; set; } = new();
    public List<string> Images { get; set; } = new();
    public int PublishedCount { get; set; }
    public int CompletedCount { get; set; }
    public int LikeCount { get; set; }
    public bool IsLiked { get; set; }
    public bool IsFavorited { get; set; }
    public bool IsDraft => Post.Status == VlogPostStatus.Draft;
    public string Highlight => ArticleContentParser.Highlight(Post.Content);
    public string PlainContent => ArticleContentParser.Intro(Post.Content);
}
