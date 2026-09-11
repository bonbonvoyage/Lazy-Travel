namespace LazyTravel.Shared.Models.EfModels;

public partial class TravelGroupInteraction
{
    public int GroupId { get; set; }

    public int MemberId { get; set; }

    public TravelGroupInteractionType ActionType { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual TravelGroup Group { get; set; } = null!;

    public virtual Member Member { get; set; } = null!;
}
