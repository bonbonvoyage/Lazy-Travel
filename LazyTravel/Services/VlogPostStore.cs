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

    static VlogPostStore()
    {
        Seed();
    }

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

    private static void Seed()
    {
        Add(new VlogPost
        {
            MemberID = 1,
            Title = "花蓮三天兩夜，慢慢晃海岸線",
            MediaUrl = "https://images.unsplash.com/photo-1500534623283-312aade485b7?w=600&q=80",
            MediaType = VlogMediaType.Photo,
            Content = "沿著台11線一路往南，行程排得很鬆，只想好好曬太陽看海。",
            Destination = "花蓮",
            TravelDays = 3,
            TravelDate = new DateOnly(2026, 5, 2),
            Status = VlogPostStatus.Published,
        });

        Add(new VlogPost
        {
            MemberID = 2,
            Title = "京都秋日散策：巷弄裡的老靈魂",
            MediaUrl = "https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?w=600&q=80",
            MediaType = VlogMediaType.Photo,
            Content = "避開觀光熱點，用五天走完自己排的私房巷弄地圖。",
            Destination = "京都",
            TravelDays = 5,
            TravelDate = new DateOnly(2025, 11, 20),
            Status = VlogPostStatus.Published,
        });

        Add(new VlogPost
        {
            MemberID = 1,
            Title = "台南巷弄美食兩天一夜",
            MediaUrl = "https://images.unsplash.com/photo-1552566626-52f8b828add9?w=600&q=80",
            MediaType = VlogMediaType.Photo,
            Content = "從早餐吃到宵夜，一份不小心會吃撐的行程表。",
            Destination = "台南",
            TravelDays = 2,
            Status = VlogPostStatus.Draft,
        });

        Add(new VlogPost
        {
            MemberID = 2,
            Title = "武陵農場露營紀錄",
            MediaUrl = "https://images.unsplash.com/photo-1504280390367-361c6d9f38f4?w=600&q=80",
            MediaType = VlogMediaType.Video,
            Content = "第一次帶新手朋友露營，紀錄裝備清單與注意事項。",
            Destination = "武陵",
            TravelDays = 2,
            Status = VlogPostStatus.Draft,
        });

        Add(new VlogPost
        {
            MemberID = 4,
            Title = "【官方精選】秋季賞楓 5 條路線推薦",
            MediaUrl = "https://images.unsplash.com/photo-1440342359743-84fcb8c21f21?w=600&q=80",
            MediaType = VlogMediaType.Photo,
            Content = "LazyTravel 團隊實地走訪整理，適合初次規劃賞楓行程的旅人參考。",
            Destination = "台灣多地",
            TravelDays = 1,
            Status = VlogPostStatus.Published,
        });

        Add(new VlogPost
        {
            MemberID = 3,
            Title = "墾丁夏日海邊放空計畫",
            MediaUrl = "https://images.unsplash.com/photo-1519046904884-53103b34b206?w=600&q=80",
            MediaType = VlogMediaType.Photo,
            Content = "白天曬海、傍晚吃海鮮，晚上到大街逛夜市，簡單但很療癒的三天。",
            Destination = "墾丁",
            TravelDays = 3,
            TravelDate = new DateOnly(2026, 7, 5),
            Status = VlogPostStatus.Published,
        });

        Add(new VlogPost
        {
            MemberID = 2,
            Title = "沖繩親子自駕 5 天 4 夜",
            MediaUrl = "https://images.unsplash.com/photo-1545579133-99bb5ab189bd?w=600&q=80",
            MediaType = VlogMediaType.Photo,
            Content = "帶小孩出國的第一次自駕嘗試，行程排得比較鬆，適合親子家庭參考。",
            Destination = "沖繩",
            TravelDays = 5,
            TravelDate = new DateOnly(2026, 4, 10),
            Status = VlogPostStatus.Published,
        });

        Add(new VlogPost
        {
            MemberID = 1,
            Title = "曼谷四天三夜按摩美食之旅（草稿中）",
            MediaUrl = "https://images.unsplash.com/photo-1508009603885-50cf7c579365?w=600&q=80",
            MediaType = VlogMediaType.Photo,
            Content = "還在整理照片跟店家資訊，行程細節之後補上。",
            Destination = "曼谷",
            TravelDays = 4,
            Status = VlogPostStatus.Draft,
        });

        Add(new VlogPost
        {
            MemberID = 3,
            Title = "首爾冬季滑雪＋逛街行程",
            MediaUrl = "https://images.unsplash.com/photo-1517154421773-0529f29ea451?w=600&q=80",
            MediaType = VlogMediaType.Photo,
            Content = "第一次滑雪就上手，順便安排了兩天逛街血拼行程。",
            Destination = "首爾",
            TravelDays = 6,
            TravelDate = new DateOnly(2026, 1, 15),
            Status = VlogPostStatus.Published,
        });

        Add(new VlogPost
        {
            MemberID = 2,
            Title = "清邁慢活咖啡廳巡禮（草稿）",
            MediaUrl = "https://images.unsplash.com/photo-1598935898639-81586f7d2129?w=600&q=80",
            MediaType = VlogMediaType.Photo,
            Content = "整理中的咖啡廳口袋名單，之後想寫成完整的一週慢活行程。",
            Destination = "清邁",
            TravelDays = 7,
            Status = VlogPostStatus.Draft,
        });

        Add(new VlogPost
        {
            MemberID = 1,
            Title = "澎湖跳島兩天一夜：花火節限定",
            MediaUrl = "https://images.unsplash.com/photo-1544644181-1484b3fdfc62?w=600&q=80",
            MediaType = VlogMediaType.Photo,
            Content = "配合花火節排的緊湊行程，跳了三個離島，晚上看煙火。",
            Destination = "澎湖",
            TravelDays = 2,
            TravelDate = new DateOnly(2026, 6, 21),
            Status = VlogPostStatus.Published,
        });

        Add(new VlogPost
        {
            MemberID = 4,
            Title = "【官方精選】九份老街半日散策地圖",
            MediaUrl = "https://images.unsplash.com/photo-1540187334920-54e87c2771c0?w=600&q=80",
            MediaType = VlogMediaType.Photo,
            Content = "LazyTravel 團隊整理的九份半日路線，避開人潮的拍照點都在這裡。",
            Destination = "九份",
            TravelDays = 1,
            Status = VlogPostStatus.Published,
        });

        // 示範一篇已軟刪除的文章，讓「已刪除」篩選頁有資料可看
        var archived = Add(new VlogPost
        {
            MemberID = 3,
            Title = "舊金山公路旅行（內容已過期）",
            MediaUrl = "https://images.unsplash.com/photo-1521464302861-ce943915d1c8?w=600&q=80",
            MediaType = VlogMediaType.Photo,
            Content = "沿1號公路一路開到優勝美地，內容已過時，先下架更新。",
            Destination = "舊金山",
            TravelDays = 7,
            Status = VlogPostStatus.Published,
        });
        Delete(archived.PostID);

        var archived2 = Add(new VlogPost
        {
            MemberID = 2,
            Title = "峇里島蜜月行（重複發文，已刪除）",
            MediaUrl = "https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=600&q=80",
            MediaType = VlogMediaType.Photo,
            Content = "不小心發了兩篇一樣的內容，這篇先刪掉留另一篇。",
            Destination = "峇里島",
            TravelDays = 5,
            Status = VlogPostStatus.Draft,
        });
        Delete(archived2.PostID);
    }
}
