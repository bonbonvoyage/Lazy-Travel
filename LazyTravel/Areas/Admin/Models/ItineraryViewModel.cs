using LazyTravel.Models;

namespace LazyTravel.Areas.Admin.Models;

public class ItineraryViewModel
{
    public VlogPost Post { get; set; } = null!;
    public List<ItineraryNode> Nodes { get; set; } = new();

    // 不為 null 時，右邊表單切成「編輯行程」模式
    public ItineraryNode? EditingNode { get; set; }
    public int EditingStayHours { get; set; }
    public int EditingStayMinutes { get; set; }
}
