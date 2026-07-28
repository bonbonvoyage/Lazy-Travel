using LazyTravel.Models;

namespace LazyTravel.Services;

// 暫時的假資料倉儲：專題目前還沒接 EF Core，先用記憶體 List 模擬 VlogPosts 資料表。
// 之後接上 EF Core 後，把這個 class 換成注入的 AppDbContext（DbSet<VlogPost> VlogPosts），
// Controller 呼叫的 GetAll / GetById / Add / Update / Delete / Restore 直接對應換掉即可。
//
// 注意：static List 只是開發階段方便，重啟網站資料就會重置，也不是執行緒安全，
// 正式環境請務必換成資料庫。
public static class VlogPostStore
{
    private static readonly List<VlogPost> _posts = new();
    private static int _nextId = 1;

    public static IReadOnlyList<VlogPost> GetAll() => _posts;

    public static VlogPost? GetById(int id) => _posts.FirstOrDefault(p => p.PostID == id);

    public static VlogPost Add(VlogPost post)
    {
        post.PostID = _nextId++;
        post.CreatedAt = DateTime.Now;
        post.UpdatedAt = null;
        post.IsDelete = false;
        _posts.Add(post);
        return post;
    }

    // 只更新允許被編輯的欄位，PostID / CreatedAt / IsDelete 不從表單覆蓋回來
    public static bool Update(VlogPost updated)
    {
        var existing = GetById(updated.PostID);
        if (existing is null)
        {
            return false;
        }

        existing.MemberID = updated.MemberID;
        existing.Title = updated.Title;
        existing.MediaUrl = updated.MediaUrl;
        existing.MediaType = updated.MediaType;
        existing.Content = updated.Content;
        existing.Destination = updated.Destination;
        existing.TravelDays = updated.TravelDays;
        existing.TravelDate = updated.TravelDate;
        existing.Status = updated.Status;
        existing.UpdatedAt = DateTime.Now;

        return true;
    }

    // 軟刪除：對應 IsDelete 欄位，不是真的從清單移除
    public static bool Delete(int id)
    {
        var post = GetById(id);
        if (post is null)
        {
            return false;
        }

        post.IsDelete = true;
        post.UpdatedAt = DateTime.Now;
        return true;
    }

    public static bool Restore(int id)
    {
        var post = GetById(id);
        if (post is null)
        {
            return false;
        }

        post.IsDelete = false;
        post.UpdatedAt = DateTime.Now;
        return true;
    }
}
