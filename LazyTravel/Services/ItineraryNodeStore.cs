using LazyTravel.Models.EfModels;

namespace LazyTravel.Services;

// 對應規格書第 13 表 ItineraryNodes 的假資料倉儲，用法同 VlogPostStore。
// 假資料本身在 VlogPostStore.Seed() 裡一併建立（那裡才拿得到剛產生文章的 PostId），
// 這裡不再自己 Seed，避免兩邊各自寫死 PostId 兜不起來。
public static class ItineraryNodeStore
{
    private static readonly List<ItineraryNode> _nodes = new();
    private static int _nextId = 1;

    public static List<ItineraryNode> GetByPostId(int postId) =>
        _nodes.Where(n => n.PostId == postId).ToList();

    public static ItineraryNode Add(ItineraryNode node)
    {
        node.NodeId = _nextId++;
        _nodes.Add(node);
        return node;
    }
}
