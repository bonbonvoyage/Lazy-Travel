using LazyTravel.Models;

namespace LazyTravel.Services;

// 對應規格書第 13 表 ItineraryNodes 的假資料倉儲，用法同 VlogPostStore。
// 假資料本身在 VlogPostStore.Seed() 裡一併建立（那裡才拿得到剛產生文章的 PostID），
// 這裡不再自己 Seed，避免兩邊各自寫死 PostID 兜不起來。
public static class ItineraryNodeStore
{
    private static readonly List<ItineraryNode> _nodes = new();
    private static int _nextId = 1;

    public static List<ItineraryNode> GetByPostId(int postId) =>
        _nodes.Where(n => n.PostID == postId).ToList();

    public static ItineraryNode? GetById(int nodeId) =>
        _nodes.FirstOrDefault(n => n.NodeID == nodeId);

    public static ItineraryNode Add(ItineraryNode node)
    {
        node.NodeID = _nextId++;
        _nodes.Add(node);
        return node;
    }

    // 只更新允許被編輯的欄位，NodeID / PostID 不從表單覆蓋回來
    public static bool Update(ItineraryNode updated)
    {
        var existing = GetById(updated.NodeID);
        if (existing is null)
        {
            return false;
        }

        existing.DayNumber = updated.DayNumber;
        existing.LocationName = updated.LocationName;
        existing.ArrivalTime = updated.ArrivalTime;
        existing.StayTime = updated.StayTime;
        existing.DepartureTime = updated.DepartureTime;
        existing.MediaUrl = updated.MediaUrl;
        existing.MediaType = updated.MediaType;
        existing.Description = updated.Description;
        existing.Remarks = updated.Remarks;
        return true;
    }

    public static bool Delete(int nodeId)
    {
        var node = _nodes.FirstOrDefault(n => n.NodeID == nodeId);
        return node is not null && _nodes.Remove(node);
    }
}
