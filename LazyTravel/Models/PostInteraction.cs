namespace LazyTravel.Models;

// 對應規格書 (0722 最新版) 第 15 表「按讚與收藏互動表 (PostInteractions)」
// 複合主鍵 (PostID, MemberID, ActionType)，在 LazyTravelContext.OnModelCreating 設定；
// 後台目前只用來讀取統計數字，不提供編輯介面。
public enum PostInteractionType
{
    Like = 1,
    Favorite = 2,
}

public class PostInteraction
{
    public int PostID { get; set; }
    public int MemberID { get; set; }
    public PostInteractionType ActionType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
