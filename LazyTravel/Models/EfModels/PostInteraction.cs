using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Models.EfModels;

// 對應規格書 (0722 最新版) 第 15 表「按讚與收藏互動表 (PostInteractions)」
// 複合主鍵 (PostId, MemberId, ActionType)，在 LazyTravelDBContext.OnModelCreating 設定；
// 後台目前只用來讀取統計數字，不提供編輯介面。
public enum PostInteractionType
{
    Like = 1,
    Favorite = 2,
}

[PrimaryKey("PostId", "MemberId", "ActionType")]
public partial class PostInteraction
{
    [Column("PostID")]
    public int PostId { get; set; }

    [Column("MemberID")]
    public int MemberId { get; set; }

    public PostInteractionType ActionType { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("MemberId")]
    [InverseProperty("PostInteractions")]
    public virtual Member? Member { get; set; }

    [ForeignKey("PostId")]
    [InverseProperty("PostInteractions")]
    public virtual VlogPost? Post { get; set; }
}
