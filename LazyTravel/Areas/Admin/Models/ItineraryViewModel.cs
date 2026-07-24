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

    public VlogPostPermissions Permissions { get; set; } = new();

    // 主管上次「退回草稿」留的原因，讓小編知道要改哪裡
    public string? LastReturnNote { get; set; }
    public DateTime? LastReturnedAt { get; set; }
}
