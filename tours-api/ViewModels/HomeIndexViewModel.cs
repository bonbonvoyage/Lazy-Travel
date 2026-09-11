namespace LazyTravel.ViewModels;

public class HomeIndexViewModel
{
    public List<HomeTrip> Trips { get; set; } = [];
    public List<HomeArticle> Articles { get; set; } = [];
    public List<string> Countries { get; set; } = [];
}
public record HomeTrip(int Id, string Title, DateOnly? Start, DateOnly? End, int People, int MaxPeople, string? Image);
public record HomeArticle(int Id, string Title, string? Image, string Author, DateTime CreatedAt);