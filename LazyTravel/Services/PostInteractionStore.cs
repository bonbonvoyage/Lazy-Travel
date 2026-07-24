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
        void AddMany(int postId, int likeCount, int favoriteCount)
        {
            for (var m = 0; m < likeCount; m++)
            {
                _interactions.Add(new PostInteraction { PostID = postId, MemberID = (m % 4) + 1, ActionType = PostInteractionType.Like });
            }
            for (var m = 0; m < favoriteCount; m++)
            {
                _interactions.Add(new PostInteraction { PostID = postId, MemberID = (m % 4) + 1, ActionType = PostInteractionType.Favorite });
            }
        }

        // (PostID, 讚數, 收藏數)。對應 VlogPostStore.Seed() 建立文章的順序（PostID 1~52，53、54 是示範用的軟刪除文章）。
        // 官方帳號（LazyTravel 官方）發的文章互動數通常比較高：5、12、22、42。
        var stats = new (int PostId, int Likes, int Favorites)[]
        {
            (1, 24, 9), (2, 35, 16), (3, 6, 2), (4, 4, 1), (5, 58, 27),
            (6, 29, 11), (7, 41, 18), (8, 9, 3), (9, 33, 15), (10, 7, 2),
            (11, 22, 9), (12, 61, 25), (13, 19, 7), (14, 15, 5), (15, 17, 6),
            (16, 26, 10), (17, 8, 3), (18, 13, 4), (19, 11, 3), (20, 6, 2),
            (21, 14, 5), (22, 47, 19), (23, 9, 3), (24, 18, 6), (25, 27, 11),
            (26, 10, 4), (27, 20, 8), (28, 38, 17), (29, 44, 20), (30, 36, 15),
            (31, 12, 4), (32, 25, 10), (33, 9, 3), (34, 31, 13), (35, 8, 2),
            (36, 21, 8), (37, 11, 4), (38, 16, 6), (39, 34, 14), (40, 7, 2),
            (41, 45, 19), (42, 66, 29), (43, 37, 16), (44, 10, 3), (45, 28, 12),
            (46, 42, 18), (47, 9, 3), (48, 20, 8), (49, 8, 2), (50, 18, 7),
            (51, 33, 14), (52, 6, 2), (53, 5, 1), (54, 0, 0),
        };

        foreach (var s in stats)
        {
            AddMany(s.PostId, s.Likes, s.Favorites);
        }
    }
}
