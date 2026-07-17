using LazyTravel.Models;

namespace LazyTravel.Services;

// 對應規格書第 14 表 PostInteractions 的假資料倉儲。
// 只提供讀取統計數字用（讚數/收藏數），不提供後台編輯——按讚/收藏本來就是前台會員的行為。
public static class PostInteractionStore
{
    private static readonly List<PostInteraction> _interactions = new();

    static PostInteractionStore()
    {
        Seed();
    }

    public static int GetLikeCount(int postId) =>
        _interactions.Count(i => i.PostID == postId && i.ActionType == PostInteractionType.Like);

    public static int GetFavoriteCount(int postId) =>
        _interactions.Count(i => i.PostID == postId && i.ActionType == PostInteractionType.Favorite);

    private static void Seed()
    {
        void AddInteraction(int postId, int memberId, PostInteractionType type) =>
            _interactions.Add(new PostInteraction { PostID = postId, MemberID = memberId, ActionType = type });

        // PostID 1：花蓮
        AddInteraction(1, 2, PostInteractionType.Like);
        AddInteraction(1, 3, PostInteractionType.Like);
        AddInteraction(1, 2, PostInteractionType.Favorite);

        // PostID 2：京都
        AddInteraction(2, 1, PostInteractionType.Like);
        AddInteraction(2, 3, PostInteractionType.Like);
        AddInteraction(2, 1, PostInteractionType.Favorite);
        AddInteraction(2, 3, PostInteractionType.Favorite);

        // PostID 5：官方賞楓精選（官方文章通常互動比較高）
        AddInteraction(5, 1, PostInteractionType.Like);
        AddInteraction(5, 2, PostInteractionType.Like);
        AddInteraction(5, 3, PostInteractionType.Like);
        AddInteraction(5, 1, PostInteractionType.Favorite);
        AddInteraction(5, 2, PostInteractionType.Favorite);

        // PostID 6：墾丁
        AddInteraction(6, 1, PostInteractionType.Like);
        AddInteraction(6, 2, PostInteractionType.Like);

        // PostID 7：沖繩
        AddInteraction(7, 1, PostInteractionType.Like);
        AddInteraction(7, 3, PostInteractionType.Like);
        AddInteraction(7, 1, PostInteractionType.Favorite);

        // PostID 9：首爾
        AddInteraction(9, 1, PostInteractionType.Like);
        AddInteraction(9, 2, PostInteractionType.Like);
        AddInteraction(9, 4, PostInteractionType.Like);
        AddInteraction(9, 2, PostInteractionType.Favorite);

        // PostID 11：澎湖
        AddInteraction(11, 2, PostInteractionType.Like);
        AddInteraction(11, 3, PostInteractionType.Like);
        AddInteraction(11, 4, PostInteractionType.Like);
        AddInteraction(11, 3, PostInteractionType.Favorite);
        AddInteraction(11, 4, PostInteractionType.Favorite);

        // PostID 12：官方九份精選
        AddInteraction(12, 1, PostInteractionType.Like);
        AddInteraction(12, 2, PostInteractionType.Like);
        AddInteraction(12, 3, PostInteractionType.Like);
        AddInteraction(12, 4, PostInteractionType.Like);
    }
}
