using LazyTravel.Models;

namespace LazyTravel.Services;

// 暫時的假資料倉儲：專題目前還沒接 EF Core，先用記憶體 List 模擬 VlogPosts 資料表。
// 之後接上 EF Core 後，把這個 class 換成注入的 AppDbContext（DbSet<VlogPost> VlogPosts），
// Controller 呼叫的 GetAll / GetById / Add / Update / Delete / Restore 直接對應換掉即可。
//
// 注意：static List 只是開發階段方便，重啟網站資料就會重置，也不是執行緒安全，
// 正式環境請務必換成資料庫。
//
// 假資料的每日行程節點（ItineraryNodes）也一併在這裡的 Seed() 建立，
// 因為只有這裡在建立文章的當下才拿得到剛產生的 PostID，避免像舊版那樣
// 在 ItineraryNodeStore 裡各自寫死 PostID = 1、2、3... 這種容易兜不起來的作法。
//
// 每篇文章的行程節點都覆蓋標題/天數宣告的每一天（例如「4 天 3 夜」就有 Day 1-4 的節點），
// 每一天至少安排兩站（符合實際排行程的情況），且每一站都帶封面圖。
// 圖片網址都先用 curl 逐一驗證過是 200 OK 才收進 StockPhotos，避免又出現載不出來的破圖。
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

    // 只有 Seed() 拿假資料用的版本，讓每篇文章可以有不同的建立/更新時間，
    // 不然假資料全部都是「剛剛」，看起來會很不真實。真的透過後台新增文章請走上面那個 Add(VlogPost)。
    private static VlogPost AddSeed(VlogPost post, DateTime createdAt, DateTime? updatedAt)
    {
        post.PostID = _nextId++;
        post.CreatedAt = createdAt;
        post.UpdatedAt = updatedAt;
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
        existing.GroupSize = updated.GroupSize;
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

    // 行程節點裡要放的一站
    private sealed record SeedStop(
        int Day, string Name, TimeOnly? Arrival, string? Stay, TimeOnly? Departure,
        string ImageUrl, string? Description = null, string? Remarks = null);

    // 一篇文章 + 底下的每日行程節點
    private sealed record SeedPost(
        int MemberId, string Title, string ImageUrl, VlogMediaType MediaType, string Content,
        string Destination, int Days, DateOnly? TravelDate, VlogPostStatus Status, TravelGroupSize GroupSize,
        SeedStop[] Stops, bool SoftDeleted = false);

    // 版權沒問題、開發階段常用的一批 Unsplash 圖庫網址。每一張都用 curl 驗證過是 200 OK，
    // 而且實際下載縮圖看過內容才分類——避免像之前那樣「寫瀑布結果配到城市夜景」的狀況。
    // 依照圖片實際內容分類成幾個主題池，Photo() 會依景點名稱關鍵字挑對應主題的圖，同主題內才輪流。
    private static readonly string[] BeachPhotos =
    {
        "https://images.unsplash.com/photo-1519046904884-53103b34b206?w=600&q=80", // 棕櫚樹沙灘+帆船
        "https://images.unsplash.com/photo-1545579133-99bb5ab189bd?w=600&q=80", // 熱帶白沙灘
        "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=600&q=80", // 海灘日出
        "https://images.unsplash.com/photo-1414609245224-afa02bfb3fda?w=600&q=80", // 海灘夕陽浪花
        "https://images.unsplash.com/photo-1509233725247-49e657c54213?w=600&q=80", // 椰子樹海灘
        "https://images.unsplash.com/photo-1512100356356-de1b84283e18?w=600&q=80", // 馬爾地夫度假村水上飛機
        "https://images.unsplash.com/photo-1499678329028-101435549a4e?w=600&q=80", // 義大利五漁村海岸小鎮夕陽
        "https://images.unsplash.com/photo-1548574505-5e239809ee19?w=600&q=80", // 熱帶港灣郵輪
        "https://images.unsplash.com/photo-1528702748617-c64d49f918af?w=600&q=80", // 沙灘+城市天際線
    };

    private static readonly string[] MountainPhotos =
    {
        "https://images.unsplash.com/photo-1500534623283-312aade485b7?w=600&q=80", // 山巒日出
        "https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?w=600&q=80", // 山中湖泊小船
        "https://images.unsplash.com/photo-1469474968028-56623f02e42e?w=600&q=80", // 山頂岩石健行
        "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=600&q=80", // 森林步道
        "https://images.unsplash.com/photo-1493246507139-91e8fad9978e?w=600&q=80", // 雪山倒映湖泊
        "https://images.unsplash.com/photo-1470770841072-f978cf4d019e?w=600&q=80", // 山中湖泊木屋
        "https://images.unsplash.com/photo-1508672019048-805c876b67e2?w=600&q=80", // 湖畔棧道靜坐
        "https://images.unsplash.com/photo-1504280390367-361c6d9f38f4?w=600&q=80", // 帳篷望向森林
        "https://images.unsplash.com/photo-1440342359743-84fcb8c21f21?w=600&q=80", // 竹林/森林仰視
    };

    private static readonly string[] WaterfallPhotos =
    {
        "https://images.unsplash.com/photo-1517760444937-f6397edcbbcd?w=600&q=80", // 叢林瀑布
    };

    private static readonly string[] TemplePhotos =
    {
        "https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?w=600&q=80", // 京都古街+五重塔
        "https://images.unsplash.com/photo-1544644181-1484b3fdfc62?w=600&q=80", // 峇里島水上寺廟
        "https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=600&q=80", // 峇里島水上寺廟（另一角度）
        "https://images.unsplash.com/photo-1528181304800-259b08848526?w=600&q=80", // 泰式金色寺廟
        "https://images.unsplash.com/photo-1526481280693-3bfa7568e0f3?w=600&q=80", // 富士山+五重塔
        "https://images.unsplash.com/photo-1490077476659-095159692ab5?w=600&q=80", // 緬甸皇家御船寺廟
    };

    private static readonly string[] PalacePhotos =
    {
        "https://images.unsplash.com/photo-1548013146-72479768bada?w=600&q=80", // 泰姬瑪哈陵拱門
    };

    private static readonly string[] StreetPhotos =
    {
        "https://images.unsplash.com/photo-1508009603885-50cf7c579365?w=600&q=80", // 曼谷夜市街景
        "https://images.unsplash.com/photo-1517154421773-0529f29ea451?w=600&q=80", // 首爾街景招牌
        "https://images.unsplash.com/photo-1540187334920-54e87c2771c0?w=600&q=80", // 九份燈籠老街
        "https://images.unsplash.com/photo-1533105079780-92b9be482077?w=600&q=80", // 聖托里尼白色階梯小巷
        "https://images.unsplash.com/photo-1471623320832-752e8bbf8413?w=600&q=80", // 巴黎街道腳踏車
    };

    private static readonly string[] CityPhotos =
    {
        "https://images.unsplash.com/photo-1598935898639-81586f7d2129?w=600&q=80", // 台北101夜景
        "https://images.unsplash.com/photo-1499856871958-5b9627545d1a?w=600&q=80", // 巴黎橋樑夜景
        "https://images.unsplash.com/photo-1502602898657-3e91760cbb34?w=600&q=80", // 艾菲爾鐵塔
        "https://images.unsplash.com/photo-1533929736458-ca588d08c8be?w=600&q=80", // 倫敦鐵橋
        "https://images.unsplash.com/photo-1524231757912-21f4fe3a7200?w=600&q=80", // 伊斯坦堡加拉達塔
        "https://images.unsplash.com/photo-1503917988258-f87a78e3c995?w=600&q=80", // 巴黎市景+艾菲爾鐵塔
        "https://images.unsplash.com/photo-1552832230-c0197dd311b5?w=600&q=80", // 羅馬競技場
        "https://images.unsplash.com/photo-1518391846015-55a9cc003b25?w=600&q=80", // 紐約橋樑夜景
        "https://images.unsplash.com/photo-1520175480921-4edfa2983e0f?w=600&q=80", // 威尼斯運河
        "https://images.unsplash.com/photo-1520986606214-8b456906c813?w=600&q=80", // 倫敦大笨鐘+紅色巴士
        "https://images.unsplash.com/photo-1583422409516-2895a77efded?w=600&q=80", // 巴塞隆納空拍
        "https://images.unsplash.com/photo-1523482580672-f109ba8cb9be?w=600&q=80", // 雪梨歌劇院夜景
        "https://images.unsplash.com/photo-1477959858617-67f85cf4f1df?w=600&q=80", // 城市天際線空拍
        "https://images.unsplash.com/photo-1483729558449-99ef09a8c325?w=600&q=80", // 里約熱內盧海灣空拍
    };

    private static readonly string[] FoodPhotos =
    {
        "https://images.unsplash.com/photo-1552566626-52f8b828add9?w=600&q=80", // 餐廳內用
        "https://images.unsplash.com/photo-1513639776629-7b61b0ac49cb?w=600&q=80", // 速食/炸雞薯條
    };

    private static readonly string[] GardenPhotos =
    {
        "https://images.unsplash.com/photo-1490750967868-88aa4486c946?w=600&q=80", // 橙色花田
    };

    private static readonly string[] BoatPhotos =
    {
        "https://images.unsplash.com/photo-1528127269322-539801943592?w=600&q=80", // 下龍灣船隻+石灰岩
    };

    private static readonly string[] AirportPhotos =
    {
        "https://images.unsplash.com/photo-1500835556837-99ac94a94552?w=600&q=80", // 飛機窗外雲海
    };

    private static readonly string[] GeneralPhotos =
    {
        "https://images.unsplash.com/photo-1469474968028-56623f02e42e?w=600&q=80",
        "https://images.unsplash.com/photo-1500534623283-312aade485b7?w=600&q=80",
        "https://images.unsplash.com/photo-1477959858617-67f85cf4f1df?w=600&q=80",
    };

    // 依景點/文章標題裡的關鍵字挑對應主題的圖庫，同一主題內用 i 輪流取，避免每次都拿同一張。
    // hint 抓不到關鍵字時退回 GeneralPhotos，至少不會牛頭不對馬嘴。
    private static string Photo(int i, string hint)
    {
        string[] pool =
            Contains(hint, "瀑布") ? WaterfallPhotos :
            Contains(hint, "神社", "寺", "廟", "教堂", "神宮") ? TemplePhotos :
            Contains(hint, "皇宮", "王宮", "皇陵", "陵寢") ? PalacePhotos :
            Contains(hint, "海灘", "海邊", "沙灘", "沙尾", "海水浴場", "潟湖", "跳島", "馬爾地夫", "水明漾") ? BeachPhotos :
            Contains(hint, "老街", "夜市", "市場", "商圈", "步行街", "街", "巷", "小徑") ? StreetPhotos :
            Contains(hint, "咖啡", "小吃", "餐廳", "美食", "蛋塔", "冬粉", "晚餐", "料理", "夜市小吃") ? FoodPhotos :
            Contains(hint, "花園", "花海", "花田", "植物園", "花公園") ? GardenPhotos :
            Contains(hint, "遊船", "獨木舟", "跳島", "遊艇", "帆船") ? BoatPhotos :
            Contains(hint, "機場", "免稅店", "登機") ? AirportPhotos :
            Contains(hint, "塔", "地標", "橋", "摩天輪", "燈塔", "天際線", "車站", "廣場", "博物館", "美術館", "文化", "展覽", "樂園", "影城", "迪士尼", "城", "宮") ? CityPhotos :
            Contains(hint, "山", "峰", "步道", "國家公園", "火山", "峽谷", "高台", "湖", "森林", "露營", "健行") ? MountainPhotos :
            GeneralPhotos;

        return pool[Math.Abs(i) % pool.Length];
    }

    private static bool Contains(string text, params string[] keywords) =>
        keywords.Any(text.Contains);

    private static void Seed()
    {
        var postIndex = 0;
        foreach (var seed in BuildSeedPosts())
        {
            postIndex++;

            // 每篇文章的建立/更新時間都錯開，不然假資料全部都是「剛剛」看起來會很假。
            // 用文章順序當隨機種子，重啟網站時間還是同一組，方便對照測試。
            var rng = new Random(postIndex * 37 + 11);
            var createdDaysAgo = rng.Next(5, 220);
            var createdAt = DateTime.Now.AddDays(-createdDaysAgo).Date
                .AddHours(rng.Next(8, 23)).AddMinutes(rng.Next(0, 60));

            DateTime? updatedAt = null;
            if (createdDaysAgo > 2 && rng.Next(0, 100) < 60)
            {
                var updatedDaysAfter = rng.Next(1, Math.Min(createdDaysAgo - 1, 60) + 1);
                updatedAt = createdAt.AddDays(updatedDaysAfter).Date
                    .AddHours(rng.Next(8, 23)).AddMinutes(rng.Next(0, 60));
            }

            var post = AddSeed(new VlogPost
            {
                MemberID = seed.MemberId,
                Title = seed.Title,
                MediaUrl = seed.ImageUrl,
                MediaType = seed.MediaType,
                Content = seed.Content,
                Destination = seed.Destination,
                TravelDays = seed.Days,
                TravelDate = seed.TravelDate,
                Status = seed.Status,
                GroupSize = seed.GroupSize,
            }, createdAt, updatedAt);

            foreach (var stop in seed.Stops)
            {
                ItineraryNodeStore.Add(new ItineraryNode
                {
                    PostID = post.PostID,
                    DayNumber = stop.Day,
                    LocationName = stop.Name,
                    ArrivalTime = stop.Arrival,
                    StayTime = stop.Stay,
                    DepartureTime = stop.Departure,
                    MediaUrl = stop.ImageUrl,
                    MediaType = VlogMediaType.Photo,
                    Description = stop.Description,
                    Remarks = stop.Remarks,
                });
            }

            if (seed.SoftDeleted)
            {
                Delete(post.PostID);
            }
        }
    }

    private static IEnumerable<SeedPost> BuildSeedPosts()
    {
        var i = 0; // 只用來輪流挑封面/節點圖片

        yield return new SeedPost(1, "花蓮三天兩夜，慢慢晃海岸線", Photo(i++, "花蓮三天兩夜，慢慢晃海岸線"), VlogMediaType.Photo,
            "沿著台11線一路往南，行程排得很鬆，只想好好曬太陽看海。",
            "花蓮", 3, new DateOnly(2026, 5, 2), VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "七星潭", new TimeOnly(9, 0), "2 小時", new TimeOnly(11, 0), Photo(i++, "七星潭"),
                    Description: "撿石頭、聽海浪聲，很適合放空。"),
                new SeedStop(1, "花蓮市區小吃", new TimeOnly(18, 0), "1.5 小時", new TimeOnly(19, 30), Photo(i++, "花蓮市區小吃"),
                    Description: "公正包子、液香扁食都在附近。"),
                new SeedStop(2, "清水斷崖", new TimeOnly(10, 0), "1 小時", new TimeOnly(11, 0), Photo(i++, "清水斷崖"),
                    Remarks: "建議在退潮時段拍照。"),
                new SeedStop(2, "太魯閣布洛灣", new TimeOnly(13, 0), "1.5 小時", new TimeOnly(14, 30), Photo(i++, "太魯閣布洛灣"),
                    Description: "台地上的原住民文化園區，視野遼闊。"),
                new SeedStop(3, "太魯閣峽谷步道", new TimeOnly(9, 0), "2 小時", new TimeOnly(11, 0), Photo(i++, "太魯閣峽谷步道"),
                    Description: "砂卡礑步道跟燕子口都值得走一趟。"),
                new SeedStop(3, "鯉魚潭", new TimeOnly(14, 0), "1.5 小時", new TimeOnly(15, 30), Photo(i++, "鯉魚潭"),
                    Description: "回程順路踩天鵝船，適合悠閒收尾。"),
            });

        yield return new SeedPost(2, "京都秋日散策：巷弄裡的老靈魂", Photo(i++, "京都秋日散策：巷弄裡的老靈魂"), VlogMediaType.Photo,
            "避開觀光熱點，用五天走完自己排的私房巷弄地圖。",
            "京都", 5, new DateOnly(2025, 11, 20), VlogPostStatus.Published, TravelGroupSize.Solo,
            new[]
            {
                new SeedStop(1, "伏見稻荷大社", new TimeOnly(8, 0), "3 小時", new TimeOnly(11, 0), Photo(i++, "伏見稻荷大社"),
                    Description: "早點到人比較少，千本鳥居很好拍。"),
                new SeedStop(1, "祇園花見小路", new TimeOnly(17, 0), "1.5 小時", new TimeOnly(18, 30), Photo(i++, "祇園花見小路"),
                    Description: "傍晚有機會遇到藝妓出門工作。"),
                new SeedStop(2, "嵐山竹林", new TimeOnly(9, 0), "1 小時", new TimeOnly(10, 0), Photo(i++, "嵐山竹林")),
                new SeedStop(2, "渡月橋", new TimeOnly(10, 30), "1 小時", new TimeOnly(11, 30), Photo(i++, "渡月橋")),
                new SeedStop(3, "銀閣寺", new TimeOnly(10, 0), "1.5 小時", new TimeOnly(11, 30), Photo(i++, "銀閣寺"),
                    Description: "枯山水庭院很適合安靜走一走。"),
                new SeedStop(3, "哲學之道", new TimeOnly(13, 0), "1.5 小時", new TimeOnly(14, 30), Photo(i++, "哲學之道"),
                    Description: "沿疏水道散步，秋天楓紅時特別美。"),
                new SeedStop(4, "錦市場", new TimeOnly(11, 0), "2 小時", new TimeOnly(13, 0), Photo(i++, "錦市場"),
                    Description: "京都的廚房，邊走邊吃剛剛好。"),
                new SeedStop(4, "二年坂三年坂", new TimeOnly(14, 0), "1.5 小時", new TimeOnly(15, 30), Photo(i++, "二年坂三年坂")),
                new SeedStop(5, "京都車站周邊伴手禮", new TimeOnly(15, 0), "2 小時", new TimeOnly(17, 0), Photo(i++, "京都車站周邊伴手禮")),
                new SeedStop(5, "東本願寺", new TimeOnly(10, 0), "1 小時", new TimeOnly(11, 0), Photo(i++, "東本願寺")),
            });

        yield return new SeedPost(1, "台南巷弄美食兩天一夜", Photo(i++, "台南巷弄美食兩天一夜"), VlogMediaType.Photo,
            "從早餐吃到宵夜，一份不小心會吃撐的行程表。",
            "台南", 2, null, VlogPostStatus.PendingReview, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "阿明豬心冬粉", new TimeOnly(7, 30), "30 分鐘", new TimeOnly(8, 0), Photo(i++, "阿明豬心冬粉"),
                    Description: "在地人推薦的早餐名店，湯頭清甜。"),
                new SeedStop(1, "神農街", new TimeOnly(15, 0), "2 小時", new TimeOnly(17, 0), Photo(i++, "神農街"),
                    Description: "老屋改建的文青小店很多，適合慢慢逛。"),
                new SeedStop(2, "花園夜市", new TimeOnly(18, 0), "2 小時", new TimeOnly(20, 0), Photo(i++, "花園夜市"),
                    Remarks: "假日限定，記得先查營業日。"),
                new SeedStop(2, "安平老街", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "安平老街"),
                    Description: "蝦餅跟豆花都是排隊名店，順路看安平古堡。"),
            });

        yield return new SeedPost(2, "武陵農場露營紀錄", Photo(i++, "武陵農場露營紀錄"), VlogMediaType.Video,
            "第一次帶新手朋友露營，紀錄裝備清單與注意事項。",
            "武陵", 2, null, VlogPostStatus.PendingReview, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "武陵農場遊客中心", new TimeOnly(13, 0), "30 分鐘", new TimeOnly(13, 30), Photo(i++, "武陵農場遊客中心"),
                    Remarks: "先辦入園手續，旺季建議提早出發。"),
                new SeedStop(1, "露營區紮營", new TimeOnly(14, 0), "3 小時", new TimeOnly(17, 0), Photo(i++, "露營區紮營"),
                    Description: "第一次搭帳篷花了不少時間，建議兩人以上一起搭。"),
                new SeedStop(2, "武陵吊橋", new TimeOnly(9, 0), "1 小時", new TimeOnly(10, 0), Photo(i++, "武陵吊橋")),
                new SeedStop(2, "桃山瀑布步道", new TimeOnly(10, 30), "2.5 小時", new TimeOnly(13, 0), Photo(i++, "桃山瀑布步道"),
                    Remarks: "來回約 8 公里，記得帶足夠飲水。"),
            });

        yield return new SeedPost(4, "【官方精選】秋季賞楓 5 條路線推薦", Photo(i++, "【官方精選】秋季賞楓 5 條路線推薦"), VlogMediaType.Photo,
            "LazyTravel 團隊實地走訪整理，適合初次規劃賞楓行程的旅人參考。",
            "台灣多地", 1, null, VlogPostStatus.Published, TravelGroupSize.Large,
            new[]
            {
                new SeedStop(1, "奧萬大森林遊樂區", new TimeOnly(9, 0), "2 小時", new TimeOnly(11, 0), Photo(i++, "奧萬大森林遊樂區"),
                    Description: "全台最著名的賞楓地點，11 月中下旬最佳。"),
                new SeedStop(1, "拉拉山恩愛農場", new TimeOnly(13, 0), "1.5 小時", new TimeOnly(14, 30), Photo(i++, "拉拉山恩愛農場")),
            });

        yield return new SeedPost(3, "墾丁夏日海邊放空計畫", Photo(i++, "墾丁夏日海邊放空計畫"), VlogMediaType.Photo,
            "白天曬海、傍晚吃海鮮，晚上到大街逛夜市，簡單但很療癒的三天。",
            "墾丁", 3, new DateOnly(2026, 7, 5), VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "白沙灣", new TimeOnly(10, 0), "3 小時", new TimeOnly(13, 0), Photo(i++, "白沙灣"),
                    Description: "沙子很細，適合浮潛，記得帶蛙鏡。"),
                new SeedStop(1, "關山夕陽", new TimeOnly(17, 30), "1 小時", new TimeOnly(18, 30), Photo(i++, "關山夕陽"),
                    Description: "全台知名夕陽觀賞點，黃昏前就要卡位。"),
                new SeedStop(2, "墾丁大街", new TimeOnly(19, 0), "2 小時", new TimeOnly(21, 0), Photo(i++, "墾丁大街")),
                new SeedStop(2, "後壁湖漁港", new TimeOnly(9, 0), "1.5 小時", new TimeOnly(10, 30), Photo(i++, "後壁湖漁港"),
                    Description: "現撈海鮮跟浮潛店都集中在這一區。"),
                new SeedStop(3, "貓鼻頭公園", new TimeOnly(9, 0), "1.5 小時", new TimeOnly(10, 30), Photo(i++, "貓鼻頭公園"),
                    Description: "珊瑚礁海蝕地形很特別，適合拍照。"),
                new SeedStop(3, "龍磐公園", new TimeOnly(11, 0), "1 小時", new TimeOnly(12, 0), Photo(i++, "龍磐公園"),
                    Description: "大草原加斷崖景觀，風大要注意安全。"),
            });

        yield return new SeedPost(2, "沖繩親子自駕 5 天 4 夜", Photo(i++, "沖繩親子自駕 5 天 4 夜"), VlogMediaType.Photo,
            "帶小孩出國的第一次自駕嘗試，行程排得比較鬆，適合親子家庭參考。",
            "沖繩", 5, new DateOnly(2026, 4, 10), VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "美國村", new TimeOnly(15, 0), "2 小時", new TimeOnly(17, 0), Photo(i++, "美國村"),
                    Description: "摩天輪跟親子友善的餐廳都在這一區。"),
                new SeedStop(1, "北谷海灘夕陽", new TimeOnly(18, 0), "1 小時", new TimeOnly(19, 0), Photo(i++, "北谷海灘夕陽")),
                new SeedStop(2, "沖繩美麗海水族館", new TimeOnly(10, 0), "3 小時", new TimeOnly(13, 0), Photo(i++, "沖繩美麗海水族館"),
                    Remarks: "建議先在官網買票，可以節省排隊時間。"),
                new SeedStop(2, "古宇利島", new TimeOnly(14, 0), "2 小時", new TimeOnly(16, 0), Photo(i++, "古宇利島"),
                    Description: "跨海大橋跟心形石都很好拍。"),
                new SeedStop(3, "沖繩兒童王國", new TimeOnly(10, 0), "2.5 小時", new TimeOnly(12, 30), Photo(i++, "沖繩兒童王國"),
                    Description: "小型動物園加遊樂設施，很適合親子放電。"),
                new SeedStop(3, "沖繩世界文化王國", new TimeOnly(14, 0), "2 小時", new TimeOnly(16, 0), Photo(i++, "沖繩世界文化王國"),
                    Description: "玉泉洞鐘乳石洞很壯觀，還有琉球文化村。"),
                new SeedStop(4, "萬座毛", new TimeOnly(15, 0), "1 小時", new TimeOnly(16, 0), Photo(i++, "萬座毛"),
                    Description: "海蝕懸崖景觀，象鼻岩很好拍。"),
                new SeedStop(4, "殘波岬燈塔", new TimeOnly(16, 30), "1 小時", new TimeOnly(17, 30), Photo(i++, "殘波岬燈塔")),
                new SeedStop(5, "那霸機場免稅店", new TimeOnly(9, 0), "2 小時", new TimeOnly(11, 0), Photo(i++, "那霸機場免稅店")),
                new SeedStop(5, "國際通伴手禮街", new TimeOnly(9, 0), "2 小時", new TimeOnly(11, 0), Photo(i++, "國際通伴手禮街")),
            });

        yield return new SeedPost(1, "曼谷四天三夜按摩美食之旅（草稿中）", Photo(i++, "曼谷四天三夜按摩美食之旅（草稿中）"), VlogMediaType.Photo,
            "還在整理照片跟店家資訊，行程細節之後補上。",
            "曼谷", 4, null, VlogPostStatus.Draft, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "考山路夜市", new TimeOnly(19, 0), "2 小時", new TimeOnly(21, 0), Photo(i++, "考山路夜市"),
                    Description: "背包客聖地，路邊按摩跟小吃都很便宜。"),
                new SeedStop(1, "鄭王廟", new TimeOnly(16, 0), "1.5 小時", new TimeOnly(17, 30), Photo(i++, "鄭王廟"),
                    Description: "湄南河畔的地標，日落時分特別美。"),
                new SeedStop(2, "大皇宮", new TimeOnly(9, 0), "2.5 小時", new TimeOnly(11, 30), Photo(i++, "大皇宮"),
                    Remarks: "服裝有規定，記得穿長褲/長裙。"),
                new SeedStop(2, "臥佛寺按摩學校", new TimeOnly(13, 0), "1.5 小時", new TimeOnly(14, 30), Photo(i++, "臥佛寺按摩學校"),
                    Description: "傳統泰式按摩發源地，價位親民。"),
                new SeedStop(3, "恰圖恰週末市集", new TimeOnly(10, 0), "3 小時", new TimeOnly(13, 0), Photo(i++, "恰圖恰週末市集"),
                    Description: "東南亞最大市集之一，很容易逛到迷路。"),
                new SeedStop(3, "暹羅商圈", new TimeOnly(14, 0), "3 小時", new TimeOnly(17, 0), Photo(i++, "暹羅商圈")),
                new SeedStop(4, "臥佛寺", new TimeOnly(9, 0), "1.5 小時", new TimeOnly(10, 30), Photo(i++, "臥佛寺")),
                new SeedStop(4, "空盛桑運河市集", new TimeOnly(11, 0), "2 小時", new TimeOnly(13, 0), Photo(i++, "空盛桑運河市集")),
            });

        yield return new SeedPost(3, "首爾冬季滑雪＋逛街行程", Photo(i++, "首爾冬季滑雪＋逛街行程"), VlogMediaType.Photo,
            "第一次滑雪就上手，順便安排了兩天逛街血拼行程。",
            "首爾", 6, new DateOnly(2026, 1, 15), VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "南怡島", new TimeOnly(9, 0), "3 小時", new TimeOnly(12, 0), Photo(i++, "南怡島")),
                new SeedStop(1, "小法國村", new TimeOnly(13, 0), "1.5 小時", new TimeOnly(14, 30), Photo(i++, "小法國村")),
                new SeedStop(2, "龍平滑雪場", new TimeOnly(9, 0), "5 小時", new TimeOnly(14, 0), Photo(i++, "龍平滑雪場"),
                    Remarks: "初學者建議先報半天教練課。"),
                new SeedStop(2, "江原道烤肉晚餐", new TimeOnly(18, 0), "1.5 小時", new TimeOnly(19, 30), Photo(i++, "江原道烤肉晚餐")),
                new SeedStop(3, "明洞", new TimeOnly(14, 0), "3 小時", new TimeOnly(17, 0), Photo(i++, "明洞"),
                    Description: "血拼跟街頭小吃的天堂，晚餐前先墊墊胃。"),
                new SeedStop(3, "南山首爾塔", new TimeOnly(18, 0), "2 小時", new TimeOnly(20, 0), Photo(i++, "南山首爾塔")),
                new SeedStop(4, "北村韓屋村", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "北村韓屋村")),
                new SeedStop(4, "景福宮", new TimeOnly(13, 0), "2 小時", new TimeOnly(15, 0), Photo(i++, "景福宮"),
                    Remarks: "整點有守門將交接儀式。"),
                new SeedStop(5, "梨泰院", new TimeOnly(18, 0), "2.5 小時", new TimeOnly(20, 30), Photo(i++, "梨泰院")),
                new SeedStop(5, "弘大商圈", new TimeOnly(14, 0), "3 小時", new TimeOnly(17, 0), Photo(i++, "弘大商圈"),
                    Description: "年輕人聚集的區域，街頭表演很熱鬧。"),
                new SeedStop(6, "仁川機場免稅購物", new TimeOnly(9, 0), "2 小時", new TimeOnly(11, 0), Photo(i++, "仁川機場免稅購物")),
                new SeedStop(6, "機場周邊超市補貨", new TimeOnly(11, 30), "1 小時", new TimeOnly(12, 30), Photo(i++, "機場周邊超市補貨")),
            });

        yield return new SeedPost(2, "清邁慢活咖啡廳巡禮（草稿）", Photo(i++, "清邁慢活咖啡廳巡禮（草稿）"), VlogMediaType.Photo,
            "整理中的咖啡廳口袋名單，之後想寫成完整的一週慢活行程。",
            "清邁", 7, null, VlogPostStatus.Draft, TravelGroupSize.Solo,
            new[]
            {
                new SeedStop(1, "寧曼路", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "寧曼路"),
                    Description: "文青咖啡廳跟選物店最密集的一區。"),
                new SeedStop(1, "塔佩門周邊晚餐", new TimeOnly(18, 0), "1.5 小時", new TimeOnly(19, 30), Photo(i++, "塔佩門周邊晚餐")),
                new SeedStop(2, "清邁古城門", new TimeOnly(9, 0), "1.5 小時", new TimeOnly(10, 30), Photo(i++, "清邁古城門")),
                new SeedStop(2, "契迪龍寺", new TimeOnly(11, 0), "1 小時", new TimeOnly(12, 0), Photo(i++, "契迪龍寺")),
                new SeedStop(3, "帕堂水牛營", new TimeOnly(9, 0), "3 小時", new TimeOnly(12, 0), Photo(i++, "帕堂水牛營"),
                    Description: "近距離接觸水牛，適合喜歡動物的人。"),
                new SeedStop(3, "叢林飛索體驗", new TimeOnly(13, 0), "2.5 小時", new TimeOnly(15, 30), Photo(i++, "叢林飛索體驗")),
                new SeedStop(4, "清邁夜間動物園", new TimeOnly(18, 0), "2.5 小時", new TimeOnly(20, 30), Photo(i++, "清邁夜間動物園")),
                new SeedStop(4, "尼曼路夜市小吃", new TimeOnly(17, 0), "1 小時", new TimeOnly(18, 0), Photo(i++, "尼曼路夜市小吃")),
                new SeedStop(5, "清邁大學周邊咖啡廳", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "清邁大學周邊咖啡廳")),
                new SeedStop(5, "清邁動物園", new TimeOnly(13, 0), "2 小時", new TimeOnly(15, 0), Photo(i++, "清邁動物園")),
                new SeedStop(6, "湄登寺", new TimeOnly(9, 0), "1 小時", new TimeOnly(10, 0), Photo(i++, "湄登寺")),
                new SeedStop(6, "素帖寺", new TimeOnly(14, 0), "2 小時", new TimeOnly(16, 0), Photo(i++, "素帖寺"),
                    Description: "搭雙條車上山，可以俯瞰整個清邁市區。"),
                new SeedStop(7, "週日夜市", new TimeOnly(17, 0), "3 小時", new TimeOnly(20, 0), Photo(i++, "週日夜市"),
                    Description: "從古城門一路擺到市中心，值得留一整晚。"),
                new SeedStop(7, "機場周邊伴手禮店", new TimeOnly(10, 0), "1.5 小時", new TimeOnly(11, 30), Photo(i++, "機場周邊伴手禮店")),
            });

        yield return new SeedPost(1, "澎湖跳島兩天一夜：花火節限定", Photo(i++, "澎湖跳島兩天一夜：花火節限定"), VlogMediaType.Photo,
            "配合花火節排的緊湊行程，跳了三個離島，晚上看煙火。",
            "澎湖", 2, new DateOnly(2026, 6, 21), VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "吉貝嶼", new TimeOnly(9, 0), "3 小時", new TimeOnly(12, 0), Photo(i++, "吉貝嶼"),
                    Description: "沙尾很漂亮，水上活動選擇也很多。"),
                new SeedStop(1, "觀音亭花火節會場", new TimeOnly(20, 0), "1 小時", new TimeOnly(21, 0), Photo(i++, "觀音亭花火節會場"),
                    Remarks: "花火節期間人潮較多，建議提早卡位。"),
                new SeedStop(2, "澎湖天堂路", new TimeOnly(9, 0), "1 小時", new TimeOnly(10, 0), Photo(i++, "澎湖天堂路"),
                    Remarks: "退潮時段才看得到完整沙洲步道。"),
                new SeedStop(2, "西嶼西臺古堡", new TimeOnly(14, 0), "1.5 小時", new TimeOnly(15, 30), Photo(i++, "西嶼西臺古堡")),
            });

        yield return new SeedPost(4, "【官方精選】九份老街半日散策地圖", Photo(i++, "【官方精選】九份老街半日散策地圖"), VlogMediaType.Photo,
            "LazyTravel 團隊整理的九份半日路線，避開人潮的拍照點都在這裡。",
            "九份", 1, null, VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "豎崎路階梯", new TimeOnly(15, 0), "1 小時", new TimeOnly(16, 0), Photo(i++, "豎崎路階梯"),
                    Description: "傍晚時分燈籠點亮，是九份最經典的拍照角度。"),
                new SeedStop(1, "阿妹茶樓", new TimeOnly(16, 30), "1 小時", new TimeOnly(17, 30), Photo(i++, "阿妹茶樓")),
            });

        // ---------- 以下為新增，補齊到 50+ 篇 ----------

        yield return new SeedPost(3, "台東熱氣球嘉年華 3 天 2 夜", Photo(i++, "台東熱氣球嘉年華 3 天 2 夜"), VlogMediaType.Photo,
            "為了看日出熱氣球特別早起，鹿野高台的視野完全值得。",
            "台東", 3, new DateOnly(2026, 7, 20), VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "鹿野高台", new TimeOnly(5, 30), "2 小時", new TimeOnly(7, 30), Photo(i++, "鹿野高台"),
                    Description: "熱氣球升空時間看天氣，建議提早到現場卡位。"),
                new SeedStop(1, "初鹿牧場", new TimeOnly(10, 0), "1.5 小時", new TimeOnly(11, 30), Photo(i++, "初鹿牧場")),
                new SeedStop(2, "伯朗大道", new TimeOnly(9, 0), "1 小時", new TimeOnly(10, 0), Photo(i++, "伯朗大道")),
                new SeedStop(2, "池上飯包文化故事館", new TimeOnly(11, 0), "1 小時", new TimeOnly(12, 0), Photo(i++, "池上飯包文化故事館")),
                new SeedStop(3, "多良車站", new TimeOnly(10, 0), "1 小時", new TimeOnly(11, 0), Photo(i++, "多良車站"),
                    Description: "有「全台最美車站」之稱，可以拍到海景月台。"),
                new SeedStop(3, "太麻里金針山", new TimeOnly(13, 0), "1.5 小時", new TimeOnly(14, 30), Photo(i++, "太麻里金針山")),
            });

        yield return new SeedPost(2, "阿里山日出芒花季 2 天 1 夜", Photo(i++, "阿里山日出芒花季 2 天 1 夜"), VlogMediaType.Photo,
            "搭小火車上山看日出雲海，順便追秋天的芒花季。",
            "嘉義阿里山", 2, new DateOnly(2025, 11, 8), VlogPostStatus.Published, TravelGroupSize.Solo,
            new[]
            {
                new SeedStop(1, "祝山觀日平台", new TimeOnly(5, 0), "1.5 小時", new TimeOnly(6, 30), Photo(i++, "祝山觀日平台"),
                    Remarks: "凌晨山上氣溫低，記得帶保暖外套。"),
                new SeedStop(1, "阿里山神木群棧道", new TimeOnly(9, 0), "2 小時", new TimeOnly(11, 0), Photo(i++, "阿里山神木群棧道")),
                new SeedStop(2, "奮起湖老街", new TimeOnly(10, 0), "1.5 小時", new TimeOnly(11, 30), Photo(i++, "奮起湖老街"),
                    Description: "老式便當跟鐵道文物館都值得一看。"),
                new SeedStop(2, "石卓老街", new TimeOnly(13, 0), "1 小時", new TimeOnly(14, 0), Photo(i++, "石卓老街")),
            });

        yield return new SeedPost(1, "日月潭單車環湖 2 天 1 夜", Photo(i++, "日月潭單車環湖 2 天 1 夜"), VlogMediaType.Photo,
            "租電動輔助車環湖一圈，坡度不算陡，新手也能輕鬆騎完。",
            "南投日月潭", 2, null, VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "向山遊客中心", new TimeOnly(9, 0), "1 小時", new TimeOnly(10, 0), Photo(i++, "向山遊客中心"),
                    Description: "清水模建築本身就很好拍，湖景一望無際。"),
                new SeedStop(1, "向山自行車道", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "向山自行車道")),
                new SeedStop(2, "文武廟", new TimeOnly(9, 0), "1 小時", new TimeOnly(10, 0), Photo(i++, "文武廟")),
                new SeedStop(2, "伊達邵老街", new TimeOnly(11, 0), "1.5 小時", new TimeOnly(12, 30), Photo(i++, "伊達邵老街"),
                    Description: "邵族部落文化跟碼頭小吃都在這一區。"),
            });

        yield return new SeedPost(3, "屏東小琉球潛水 3 天 2 夜", Photo(i++, "屏東小琉球潛水 3 天 2 夜"), VlogMediaType.Photo,
            "第一次浮潛就遇到海龜，能見度好到嚇人。",
            "屏東小琉球", 3, new DateOnly(2026, 8, 2), VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "美人洞", new TimeOnly(9, 0), "1.5 小時", new TimeOnly(10, 30), Photo(i++, "美人洞")),
                new SeedStop(1, "山豬溝", new TimeOnly(11, 0), "1 小時", new TimeOnly(12, 0), Photo(i++, "山豬溝")),
                new SeedStop(2, "杉福漁港浮潛區", new TimeOnly(8, 0), "2 小時", new TimeOnly(10, 0), Photo(i++, "杉福漁港浮潛區"),
                    Description: "運氣好幾乎每次下水都能看到海龜。"),
                new SeedStop(2, "美人洞夜間導覽", new TimeOnly(19, 0), "1.5 小時", new TimeOnly(20, 30), Photo(i++, "美人洞夜間導覽"),
                    Description: "晚上找陸蟹跟寄居蟹，親子也適合。"),
                new SeedStop(3, "花瓶岩", new TimeOnly(7, 0), "1 小時", new TimeOnly(8, 0), Photo(i++, "花瓶岩"),
                    Description: "日出時段的花瓶岩最有名，順便看日出。"),
                new SeedStop(3, "白沙尾漁港", new TimeOnly(9, 0), "1 小時", new TimeOnly(10, 0), Photo(i++, "白沙尾漁港")),
            });

        yield return new SeedPost(2, "宜蘭礁溪溫泉泡湯 2 天 1 夜", Photo(i++, "宜蘭礁溪溫泉泡湯 2 天 1 夜"), VlogMediaType.Photo,
            "平價溫泉小旅行，晚上順便去逛觀光夜市。",
            "宜蘭", 2, null, VlogPostStatus.Draft, TravelGroupSize.Solo,
            new[]
            {
                new SeedStop(1, "礁溪溫泉公園", new TimeOnly(16, 0), "1.5 小時", new TimeOnly(17, 30), Photo(i++, "礁溪溫泉公園")),
                new SeedStop(1, "湯圍溝公園", new TimeOnly(14, 0), "1 小時", new TimeOnly(15, 0), Photo(i++, "湯圍溝公園"),
                    Description: "免費足湯，逛街逛累了可以順路泡一下。"),
                new SeedStop(2, "羅東夜市", new TimeOnly(18, 0), "2 小時", new TimeOnly(20, 0), Photo(i++, "羅東夜市"),
                    Description: "包心粉圓、羊肉湯都是必吃名店。"),
                new SeedStop(2, "羅東林業文化園區", new TimeOnly(10, 0), "1.5 小時", new TimeOnly(11, 30), Photo(i++, "羅東林業文化園區")),
            });

        yield return new SeedPost(1, "高雄駁二藝術特區美食一日遊", Photo(i++, "高雄駁二藝術特區美食一日遊"), VlogMediaType.Photo,
            "從文創園區逛到興達港夜景，適合排一天輕鬆走走。",
            "高雄", 1, null, VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "駁二藝術特區", new TimeOnly(14, 0), "2 小時", new TimeOnly(16, 0), Photo(i++, "駁二藝術特區"),
                    Description: "常態展跟裝置藝術都很好拍照。"),
                new SeedStop(1, "興達港", new TimeOnly(17, 0), "1.5 小時", new TimeOnly(18, 30), Photo(i++, "興達港"),
                    Description: "漁市場加夕陽，晚餐可以直接吃現撈海鮮。"),
            });

        yield return new SeedPost(3, "台中新社花海一日遊", Photo(i++, "台中新社花海一日遊"), VlogMediaType.Photo,
            "花季限定的一日輕旅行，順便吃了附近的菇類料理。",
            "台中", 1, new DateOnly(2025, 11, 15), VlogPostStatus.Published, TravelGroupSize.Large,
            new[]
            {
                new SeedStop(1, "新社花海會場", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "新社花海會場")),
                new SeedStop(1, "新社菇菇餐廳", new TimeOnly(12, 30), "1.5 小時", new TimeOnly(14, 0), Photo(i++, "新社菇菇餐廳"),
                    Description: "在地特色菇類料理，套餐選擇很多。"),
            });

        yield return new SeedPost(2, "苗栗桐花祭賞花 2 天 1 夜", Photo(i++, "苗栗桐花祭賞花 2 天 1 夜"), VlogMediaType.Photo,
            "五月雪盛開時上山賞桐花，順道走了幾條古道。",
            "苗栗", 2, new DateOnly(2026, 5, 3), VlogPostStatus.Draft, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "桐花公園", new TimeOnly(10, 0), "1.5 小時", new TimeOnly(11, 30), Photo(i++, "桐花公園")),
                new SeedStop(1, "南庄老街", new TimeOnly(13, 0), "2 小時", new TimeOnly(15, 0), Photo(i++, "南庄老街")),
                new SeedStop(2, "勝興車站", new TimeOnly(10, 0), "1.5 小時", new TimeOnly(11, 30), Photo(i++, "勝興車站"),
                    Description: "舊山線鐵道文化保存得很完整。"),
                new SeedStop(2, "龍騰斷橋", new TimeOnly(12, 0), "1 小時", new TimeOnly(13, 0), Photo(i++, "龍騰斷橋")),
            });

        yield return new SeedPost(1, "基隆廟口夜市小吃地圖", Photo(i++, "基隆廟口夜市小吃地圖"), VlogMediaType.Photo,
            "從天婦羅吃到泡泡冰，整理了一份不會踩雷的排隊名店清單。",
            "基隆", 1, null, VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "廟口夜市", new TimeOnly(18, 0), "2 小時", new TimeOnly(20, 0), Photo(i++, "廟口夜市"),
                    Description: "天婦羅、鼎邊趖、泡泡冰都在主要通道上。"),
                new SeedStop(1, "正濱漁港彩色屋", new TimeOnly(16, 0), "1 小時", new TimeOnly(17, 0), Photo(i++, "正濱漁港彩色屋"),
                    Description: "有「基隆版威尼斯」之稱，適合拍照。"),
            });

        yield return new SeedPost(4, "【官方精選】淡水老街夕陽散步路線", Photo(i++, "【官方精選】淡水老街夕陽散步路線"), VlogMediaType.Photo,
            "LazyTravel 團隊整理的淡水半日路線，河岸夕陽是重頭戲。",
            "淡水", 1, null, VlogPostStatus.Published, TravelGroupSize.Large,
            new[]
            {
                new SeedStop(1, "淡水老街", new TimeOnly(15, 0), "1.5 小時", new TimeOnly(16, 30), Photo(i++, "淡水老街")),
                new SeedStop(1, "淡水河岸公園", new TimeOnly(17, 30), "1 小時", new TimeOnly(18, 30), Photo(i++, "淡水河岸公園"),
                    Description: "夕陽時段是全台熱門的拍照時刻。"),
            });

        yield return new SeedPost(2, "東北角海岸線兜風 2 天 1 夜", Photo(i++, "東北角海岸線兜風 2 天 1 夜"), VlogMediaType.Photo,
            "沿著濱海公路一路開，中途停了好幾個祕境海灣拍照。",
            "東北角", 2, null, VlogPostStatus.Draft, TravelGroupSize.Solo,
            new[]
            {
                new SeedStop(1, "龍洞灣岬步道", new TimeOnly(10, 0), "1.5 小時", new TimeOnly(11, 30), Photo(i++, "龍洞灣岬步道")),
                new SeedStop(1, "南雅奇岩", new TimeOnly(13, 0), "1 小時", new TimeOnly(14, 0), Photo(i++, "南雅奇岩")),
                new SeedStop(2, "鼻頭角步道", new TimeOnly(9, 0), "1.5 小時", new TimeOnly(10, 30), Photo(i++, "鼻頭角步道"),
                    Description: "海蝕地形教科書，步道沿途都是海景。"),
                new SeedStop(2, "深澳漁港岬角步道", new TimeOnly(11, 0), "1 小時", new TimeOnly(12, 0), Photo(i++, "深澳漁港岬角步道")),
            });

        yield return new SeedPost(3, "金門戰地文化巡禮 3 天 2 夜", Photo(i++, "金門戰地文化巡禮 3 天 2 夜"), VlogMediaType.Photo,
            "走訪坑道跟戰史館，順便嚐了道地的高粱風味料理。",
            "金門", 3, new DateOnly(2026, 3, 12), VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "翟山坑道", new TimeOnly(9, 0), "1 小時", new TimeOnly(10, 0), Photo(i++, "翟山坑道"),
                    Description: "人工開鑿的水道坑道，內部相當壯觀。"),
                new SeedStop(1, "金門酒廠", new TimeOnly(14, 0), "1 小時", new TimeOnly(15, 0), Photo(i++, "金門酒廠")),
                new SeedStop(2, "金門古寧頭戰史館", new TimeOnly(13, 0), "1.5 小時", new TimeOnly(14, 30), Photo(i++, "金門古寧頭戰史館")),
                new SeedStop(2, "慈湖三角堡", new TimeOnly(16, 0), "1 小時", new TimeOnly(17, 0), Photo(i++, "慈湖三角堡"),
                    Description: "傍晚常有鸕鶿歸巢的畫面可以拍。"),
                new SeedStop(3, "金門風獅爺文化館", new TimeOnly(10, 0), "1 小時", new TimeOnly(11, 0), Photo(i++, "金門風獅爺文化館"),
                    Description: "認識金門特有的風獅爺信仰文化。"),
                new SeedStop(3, "水頭聚落", new TimeOnly(11, 30), "1.5 小時", new TimeOnly(13, 0), Photo(i++, "水頭聚落"),
                    Description: "閩南與洋樓建築混搭，很好拍照。"),
            });

        yield return new SeedPost(1, "馬祖藍眼淚追星 3 天 2 夜", Photo(i++, "馬祖藍眼淚追星 3 天 2 夜"), VlogMediaType.Photo,
            "特地挑無月光的夜晚出海，藍眼淚跟星空同時入鏡。",
            "馬祖", 3, new DateOnly(2026, 5, 18), VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "大坵島", new TimeOnly(15, 0), "2 小時", new TimeOnly(17, 0), Photo(i++, "大坵島")),
                new SeedStop(1, "芹壁聚落", new TimeOnly(10, 0), "1.5 小時", new TimeOnly(11, 30), Photo(i++, "芹壁聚落"),
                    Description: "地中海風格石屋，有「馬祖地中海」之稱。"),
                new SeedStop(2, "鐵板燒藍眼淚觀測點", new TimeOnly(20, 0), "1.5 小時", new TimeOnly(21, 30), Photo(i++, "鐵板燒藍眼淚觀測點"),
                    Remarks: "需配合潮汐跟天氣，建議提前查預報。"),
                new SeedStop(2, "馬祖故事館", new TimeOnly(10, 0), "1 小時", new TimeOnly(11, 0), Photo(i++, "馬祖故事館")),
                new SeedStop(3, "北海坑道", new TimeOnly(10, 0), "1 小時", new TimeOnly(11, 0), Photo(i++, "北海坑道"),
                    Description: "戰地時期開鑿的水上坑道，划舢舨進去很特別。"),
                new SeedStop(3, "馬祖燈塔", new TimeOnly(13, 0), "1 小時", new TimeOnly(14, 0), Photo(i++, "馬祖燈塔")),
            });

        yield return new SeedPost(2, "綠島潛水環島 3 天 2 夜", Photo(i++, "綠島潛水環島 3 天 2 夜"), VlogMediaType.Photo,
            "考了自由潛水體驗證，環島公路騎起來風景超讚。",
            "綠島", 3, null, VlogPostStatus.Draft, TravelGroupSize.Solo,
            new[]
            {
                new SeedStop(1, "朝日溫泉", new TimeOnly(5, 0), "1.5 小時", new TimeOnly(6, 30), Photo(i++, "朝日溫泉"),
                    Description: "全球少見的海底溫泉，日出時段最推薦。"),
                new SeedStop(1, "綠島人權文化園區", new TimeOnly(10, 0), "1.5 小時", new TimeOnly(11, 30), Photo(i++, "綠島人權文化園區")),
                new SeedStop(2, "柚子湖", new TimeOnly(9, 0), "1 小時", new TimeOnly(10, 0), Photo(i++, "柚子湖"),
                    Description: "廢棄聚落遺跡，適合拍照跟散步。"),
                new SeedStop(2, "自由潛水體驗課", new TimeOnly(14, 0), "3 小時", new TimeOnly(17, 0), Photo(i++, "自由潛水體驗課")),
                new SeedStop(3, "綠島燈塔", new TimeOnly(10, 0), "1 小時", new TimeOnly(11, 0), Photo(i++, "綠島燈塔")),
                new SeedStop(3, "大白沙潛水區", new TimeOnly(13, 0), "2 小時", new TimeOnly(15, 0), Photo(i++, "大白沙潛水區")),
            });

        yield return new SeedPost(3, "蘭嶼飛魚季 4 天 3 夜", Photo(i++, "蘭嶼飛魚季 4 天 3 夜"), VlogMediaType.Photo,
            "跟著在地達悟族朋友體驗傳統拼板舟文化。",
            "蘭嶼", 4, new DateOnly(2026, 4, 25), VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "開元港", new TimeOnly(9, 0), "1 小時", new TimeOnly(10, 0), Photo(i++, "開元港")),
                new SeedStop(1, "蘭嶼氣象站", new TimeOnly(11, 0), "1 小時", new TimeOnly(12, 0), Photo(i++, "蘭嶼氣象站")),
                new SeedStop(2, "野銀部落地下屋", new TimeOnly(14, 0), "1.5 小時", new TimeOnly(15, 30), Photo(i++, "野銀部落地下屋"),
                    Description: "傳統達悟族地下屋建築，冬暖夏涼很有智慧。"),
                new SeedStop(2, "拼板舟體驗", new TimeOnly(9, 0), "2 小時", new TimeOnly(11, 0), Photo(i++, "拼板舟體驗")),
                new SeedStop(3, "情人洞", new TimeOnly(10, 0), "1 小時", new TimeOnly(11, 0), Photo(i++, "情人洞")),
                new SeedStop(3, "軍艦岩", new TimeOnly(15, 0), "1 小時", new TimeOnly(16, 0), Photo(i++, "軍艦岩")),
                new SeedStop(4, "紅頭部落", new TimeOnly(9, 0), "1.5 小時", new TimeOnly(10, 30), Photo(i++, "紅頭部落"),
                    Description: "近距離感受達悟族傳統生活方式。"),
                new SeedStop(4, "蘭嶼燈塔", new TimeOnly(13, 0), "1 小時", new TimeOnly(14, 0), Photo(i++, "蘭嶼燈塔")),
            });

        yield return new SeedPost(1, "大阪環球影城親子遊 4 天 3 夜", Photo(i++, "大阪環球影城親子遊 4 天 3 夜"), VlogMediaType.Photo,
            "帶小朋友第一次玩哈利波特園區，排隊策略整理給大家參考。",
            "大阪", 4, new DateOnly(2026, 3, 28), VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "哈利波特魔法世界", new TimeOnly(9, 0), "4 小時", new TimeOnly(13, 0), Photo(i++, "哈利波特魔法世界"),
                    Remarks: "建議一開園先衝快速通關，中午人潮最多。"),
                new SeedStop(1, "小小兵樂園", new TimeOnly(14, 0), "2 小時", new TimeOnly(16, 0), Photo(i++, "小小兵樂園")),
                new SeedStop(2, "道頓堀", new TimeOnly(18, 0), "2 小時", new TimeOnly(20, 0), Photo(i++, "道頓堀")),
                new SeedStop(2, "心齋橋", new TimeOnly(15, 0), "2 小時", new TimeOnly(17, 0), Photo(i++, "心齋橋")),
                new SeedStop(3, "大阪城公園", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "大阪城公園")),
                new SeedStop(3, "梅田空中庭園展望台", new TimeOnly(18, 0), "1.5 小時", new TimeOnly(19, 30), Photo(i++, "梅田空中庭園展望台")),
                new SeedStop(4, "黑門市場", new TimeOnly(10, 0), "1.5 小時", new TimeOnly(11, 30), Photo(i++, "黑門市場"),
                    Description: "大阪人的廚房，可以邊走邊吃海鮮。"),
                new SeedStop(4, "關西機場伴手禮", new TimeOnly(13, 0), "1.5 小時", new TimeOnly(14, 30), Photo(i++, "關西機場伴手禮")),
            });

        yield return new SeedPost(2, "東京淺草晴空塔 5 天 4 夜", Photo(i++, "東京淺草晴空塔 5 天 4 夜"), VlogMediaType.Photo,
            "老城區跟新地標一次收集，順便安排了兩天近郊小旅行。",
            "東京", 5, new DateOnly(2026, 2, 14), VlogPostStatus.Published, TravelGroupSize.Solo,
            new[]
            {
                new SeedStop(1, "淺草寺", new TimeOnly(8, 30), "2 小時", new TimeOnly(10, 30), Photo(i++, "淺草寺"),
                    Description: "仲見世通商店街很好逛，早點到人比較少。"),
                new SeedStop(1, "東京晴空塔", new TimeOnly(15, 0), "2 小時", new TimeOnly(17, 0), Photo(i++, "東京晴空塔")),
                new SeedStop(2, "明治神宮", new TimeOnly(9, 0), "1.5 小時", new TimeOnly(10, 30), Photo(i++, "明治神宮")),
                new SeedStop(2, "原宿竹下通", new TimeOnly(11, 0), "2 小時", new TimeOnly(13, 0), Photo(i++, "原宿竹下通")),
                new SeedStop(3, "澀谷十字路口", new TimeOnly(19, 0), "1.5 小時", new TimeOnly(20, 30), Photo(i++, "澀谷十字路口")),
                new SeedStop(3, "新宿御苑", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "新宿御苑")),
                new SeedStop(4, "箱根溫泉一日遊", new TimeOnly(9, 0), "8 小時", new TimeOnly(17, 0), Photo(i++, "箱根溫泉一日遊"),
                    Remarks: "可搭箱根周遊券，順路看富士山。"),
                new SeedStop(4, "箱根神社", new TimeOnly(13, 0), "1 小時", new TimeOnly(14, 0), Photo(i++, "箱根神社")),
                new SeedStop(5, "秋葉原", new TimeOnly(10, 0), "3 小時", new TimeOnly(13, 0), Photo(i++, "秋葉原")),
                new SeedStop(5, "東京車站伴手禮", new TimeOnly(14, 0), "1.5 小時", new TimeOnly(15, 30), Photo(i++, "東京車站伴手禮")),
            });

        yield return new SeedPost(3, "北海道富良野花海 5 天 4 夜", Photo(i++, "北海道富良野花海 5 天 4 夜"), VlogMediaType.Photo,
            "夏天的薰衣草花田美到不真實，順便安排了函館夜景。",
            "北海道", 5, new DateOnly(2026, 7, 10), VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "富田農場", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "富田農場"),
                    Description: "七月中是薰衣草最盛開的時期。"),
                new SeedStop(1, "四季彩之丘", new TimeOnly(14, 0), "1.5 小時", new TimeOnly(15, 30), Photo(i++, "四季彩之丘")),
                new SeedStop(2, "美瑛青池", new TimeOnly(9, 0), "1 小時", new TimeOnly(10, 0), Photo(i++, "美瑛青池")),
                new SeedStop(2, "拼布之路", new TimeOnly(11, 0), "2 小時", new TimeOnly(13, 0), Photo(i++, "拼布之路"),
                    Description: "沿路都是像拼布般的丘陵田野景觀。"),
                new SeedStop(3, "函館山夜景", new TimeOnly(19, 0), "1.5 小時", new TimeOnly(20, 30), Photo(i++, "函館山夜景")),
                new SeedStop(3, "函館朝市", new TimeOnly(8, 0), "1.5 小時", new TimeOnly(9, 30), Photo(i++, "函館朝市")),
                new SeedStop(4, "小樽運河", new TimeOnly(14, 0), "2 小時", new TimeOnly(16, 0), Photo(i++, "小樽運河")),
                new SeedStop(4, "小樽玻璃工房", new TimeOnly(16, 30), "1 小時", new TimeOnly(17, 30), Photo(i++, "小樽玻璃工房")),
                new SeedStop(5, "札幌薄野", new TimeOnly(18, 0), "2 小時", new TimeOnly(20, 0), Photo(i++, "札幌薄野"),
                    Description: "北海道最熱鬧的美食夜生活區。"),
                new SeedStop(5, "札幌大通公園", new TimeOnly(10, 0), "1 小時", new TimeOnly(11, 0), Photo(i++, "札幌大通公園")),
            });

        yield return new SeedPost(1, "沖繩石垣島跳島 4 天 3 夜", Photo(i++, "沖繩石垣島跳島 4 天 3 夜"), VlogMediaType.Photo,
            "包船跳了三個離島，川平灣的海水藍到很不真實。",
            "石垣島", 4, null, VlogPostStatus.Draft, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "川平灣", new TimeOnly(10, 0), "1.5 小時", new TimeOnly(11, 30), Photo(i++, "川平灣")),
                new SeedStop(1, "石垣島市區牛排晚餐", new TimeOnly(18, 0), "1.5 小時", new TimeOnly(19, 30), Photo(i++, "石垣島市區牛排晚餐")),
                new SeedStop(2, "竹富島", new TimeOnly(9, 0), "3 小時", new TimeOnly(12, 0), Photo(i++, "竹富島"),
                    Description: "水牛車繞行紅瓦聚落，很有沖繩傳統風情。"),
                new SeedStop(2, "西表島紅樹林獨木舟", new TimeOnly(9, 0), "3 小時", new TimeOnly(12, 0), Photo(i++, "西表島紅樹林獨木舟")),
                new SeedStop(3, "石垣島離島碼頭市場", new TimeOnly(10, 0), "1.5 小時", new TimeOnly(11, 30), Photo(i++, "石垣島離島碼頭市場")),
                new SeedStop(3, "米原海灘浮潛", new TimeOnly(13, 0), "2 小時", new TimeOnly(15, 0), Photo(i++, "米原海灘浮潛")),
                new SeedStop(4, "石垣島鐘乳石洞", new TimeOnly(10, 0), "1 小時", new TimeOnly(11, 0), Photo(i++, "石垣島鐘乳石洞")),
                new SeedStop(4, "石垣機場伴手禮", new TimeOnly(12, 0), "1.5 小時", new TimeOnly(13, 30), Photo(i++, "石垣機場伴手禮")),
            });

        yield return new SeedPost(2, "釜山海雲台自由行 4 天 3 夜", Photo(i++, "釜山海雲台自由行 4 天 3 夜"), VlogMediaType.Photo,
            "海邊咖啡廳配夕陽，比首爾多了一份悠閒感。",
            "釜山", 4, new DateOnly(2026, 6, 5), VlogPostStatus.Published, TravelGroupSize.Solo,
            new[]
            {
                new SeedStop(1, "海雲台海水浴場", new TimeOnly(16, 0), "2 小時", new TimeOnly(18, 0), Photo(i++, "海雲台海水浴場")),
                new SeedStop(1, "海東龍宮寺", new TimeOnly(9, 0), "1.5 小時", new TimeOnly(10, 30), Photo(i++, "海東龍宮寺"),
                    Description: "少數建在海邊的寺廟，早上光線最美。"),
                new SeedStop(2, "甘川文化村", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "甘川文化村"),
                    Description: "彩色階梯屋很好拍，建議穿好走的鞋子。"),
                new SeedStop(2, "廣安大橋夜景", new TimeOnly(19, 0), "1.5 小時", new TimeOnly(20, 30), Photo(i++, "廣安大橋夜景")),
                new SeedStop(3, "太宗台", new TimeOnly(9, 0), "2 小時", new TimeOnly(11, 0), Photo(i++, "太宗台")),
                new SeedStop(3, "松島海上纜車", new TimeOnly(14, 0), "1.5 小時", new TimeOnly(15, 30), Photo(i++, "松島海上纜車")),
                new SeedStop(4, "札嘎其市場", new TimeOnly(11, 0), "1.5 小時", new TimeOnly(12, 30), Photo(i++, "札嘎其市場"),
                    Description: "釜山最大海鮮市場，可以現點現煮。"),
                new SeedStop(4, "釜山電影殿堂", new TimeOnly(13, 0), "1 小時", new TimeOnly(14, 0), Photo(i++, "釜山電影殿堂")),
            });

        yield return new SeedPost(3, "濟州島火山島地質公園 3 天 2 夜", Photo(i++, "濟州島火山島地質公園 3 天 2 夜"), VlogMediaType.Photo,
            "租車環島兩天，城山日出峰的日出值得早起。",
            "濟州島", 3, null, VlogPostStatus.Draft, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "城山日出峰", new TimeOnly(5, 30), "1.5 小時", new TimeOnly(7, 0), Photo(i++, "城山日出峰"),
                    Remarks: "登頂約需 30-40 分鐘，建議穿好走的鞋。"),
                new SeedStop(1, "牛島渡輪一日遊", new TimeOnly(9, 0), "5 小時", new TimeOnly(14, 0), Photo(i++, "牛島渡輪一日遊")),
                new SeedStop(2, "萬丈窟", new TimeOnly(10, 0), "1.5 小時", new TimeOnly(11, 30), Photo(i++, "萬丈窟"),
                    Description: "世界最長的熔岩洞窟之一，內部涼爽。"),
                new SeedStop(2, "漢拏山國立公園", new TimeOnly(13, 0), "3 小時", new TimeOnly(16, 0), Photo(i++, "漢拏山國立公園")),
                new SeedStop(3, "涉地可支", new TimeOnly(9, 0), "1 小時", new TimeOnly(10, 0), Photo(i++, "涉地可支")),
                new SeedStop(3, "濟州機場伴手禮", new TimeOnly(11, 0), "1.5 小時", new TimeOnly(12, 30), Photo(i++, "濟州機場伴手禮")),
            });

        yield return new SeedPost(1, "新加坡聖淘沙親子遊 3 天 2 夜", Photo(i++, "新加坡聖淘沙親子遊 3 天 2 夜"), VlogMediaType.Photo,
            "環球影城加海洋世界，兩天玩到腿軟但小孩超開心。",
            "新加坡", 3, new DateOnly(2026, 8, 15), VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "聖淘沙環球影城", new TimeOnly(9, 0), "5 小時", new TimeOnly(14, 0), Photo(i++, "聖淘沙環球影城")),
                new SeedStop(1, "S.E.A. 海洋館", new TimeOnly(15, 0), "2 小時", new TimeOnly(17, 0), Photo(i++, "S.E.A. 海洋館")),
                new SeedStop(2, "濱海灣花園", new TimeOnly(18, 0), "2 小時", new TimeOnly(20, 0), Photo(i++, "濱海灣花園"),
                    Description: "超級樹夜間燈光秀不能錯過。"),
                new SeedStop(2, "魚尾獅公園", new TimeOnly(16, 0), "1 小時", new TimeOnly(17, 0), Photo(i++, "魚尾獅公園")),
                new SeedStop(3, "牛車水", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "牛車水"),
                    Description: "新加坡的中國城，小吃跟伴手禮都很好買。"),
                new SeedStop(3, "小印度", new TimeOnly(13, 0), "1.5 小時", new TimeOnly(14, 30), Photo(i++, "小印度")),
            });

        yield return new SeedPost(2, "吉隆坡雙子星城市巡禮 3 天 2 夜", Photo(i++, "吉隆坡雙子星城市巡禮 3 天 2 夜"), VlogMediaType.Photo,
            "白天逛市集，晚上到雙子星塔下拍夜景，物價相對親民。",
            "吉隆坡", 3, null, VlogPostStatus.Draft, TravelGroupSize.Solo,
            new[]
            {
                new SeedStop(1, "雙子星塔", new TimeOnly(19, 0), "1.5 小時", new TimeOnly(20, 30), Photo(i++, "雙子星塔")),
                new SeedStop(1, "武吉免登購物區", new TimeOnly(14, 0), "3 小時", new TimeOnly(17, 0), Photo(i++, "武吉免登購物區")),
                new SeedStop(2, "馬六甲一日遊", new TimeOnly(8, 0), "9 小時", new TimeOnly(17, 0), Photo(i++, "馬六甲一日遊"),
                    Remarks: "來回車程較長，建議報一日團比較省事。"),
                new SeedStop(2, "武吉免登夜市小吃", new TimeOnly(19, 0), "1.5 小時", new TimeOnly(20, 30), Photo(i++, "武吉免登夜市小吃")),
                new SeedStop(3, "黑風洞", new TimeOnly(9, 0), "1.5 小時", new TimeOnly(10, 30), Photo(i++, "黑風洞"),
                    Description: "272 階彩色階梯加印度教石窟廟宇。"),
                new SeedStop(3, "獨立廣場", new TimeOnly(13, 0), "1.5 小時", new TimeOnly(14, 30), Photo(i++, "獨立廣場")),
            });

        yield return new SeedPost(3, "越南峴港美溪海灘假期 4 天 3 夜", Photo(i++, "越南峴港美溪海灘假期 4 天 3 夜"), VlogMediaType.Photo,
            "海灘度假村躺整天，中間插一天去了會安古鎮。",
            "峴港", 4, new DateOnly(2026, 9, 1), VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "美溪海灘", new TimeOnly(9, 0), "3 小時", new TimeOnly(12, 0), Photo(i++, "美溪海灘")),
                new SeedStop(1, "五行山", new TimeOnly(14, 0), "2 小時", new TimeOnly(16, 0), Photo(i++, "五行山")),
                new SeedStop(2, "會安古鎮燈籠街", new TimeOnly(18, 0), "2 小時", new TimeOnly(20, 0), Photo(i++, "會安古鎮燈籠街"),
                    Description: "傍晚點燈後整條河岸都是燈籠倒影，很好拍。"),
                new SeedStop(2, "會安古鎮日間老街", new TimeOnly(14, 0), "2 小時", new TimeOnly(16, 0), Photo(i++, "會安古鎮日間老街")),
                new SeedStop(3, "巴拿山法國村", new TimeOnly(9, 0), "6 小時", new TimeOnly(15, 0), Photo(i++, "巴拿山法國村"),
                    Remarks: "金橋很熱門，建議一早搭纜車上山。"),
                new SeedStop(3, "美溪海灘夜市晚餐", new TimeOnly(18, 0), "1.5 小時", new TimeOnly(19, 30), Photo(i++, "美溪海灘夜市晚餐")),
                new SeedStop(4, "峴港龍橋夜市", new TimeOnly(19, 0), "1.5 小時", new TimeOnly(20, 30), Photo(i++, "峴港龍橋夜市"),
                    Description: "週末晚上龍橋會噴火噴水，記得抓好時間。"),
                new SeedStop(4, "占婆博物館", new TimeOnly(10, 0), "1 小時", new TimeOnly(11, 0), Photo(i++, "占婆博物館")),
            });

        yield return new SeedPost(1, "越南河內下龍灣遊船 5 天 4 夜", Photo(i++, "越南河內下龍灣遊船 5 天 4 夜"), VlogMediaType.Photo,
            "在船上住了一晚，睡醒直接看到石灰岩奇景，很值得。",
            "河內", 5, null, VlogPostStatus.Draft, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "還劍湖", new TimeOnly(17, 0), "1.5 小時", new TimeOnly(18, 30), Photo(i++, "還劍湖")),
                new SeedStop(1, "河內大教堂", new TimeOnly(15, 0), "1 小時", new TimeOnly(16, 0), Photo(i++, "河內大教堂")),
                new SeedStop(2, "下龍市海鮮市場", new TimeOnly(9, 0), "1 小時", new TimeOnly(10, 0), Photo(i++, "下龍市海鮮市場")),
                new SeedStop(2, "下龍灣遊船", new TimeOnly(11, 0), "6 小時", new TimeOnly(17, 0), Photo(i++, "下龍灣遊船"),
                    Remarks: "建議選有含晚餐跟隔天早餐的行程。"),
                new SeedStop(3, "驚訝洞", new TimeOnly(9, 0), "1.5 小時", new TimeOnly(10, 30), Photo(i++, "驚訝洞"),
                    Description: "下龍灣船上行程常包含的鐘乳石洞景點。"),
                new SeedStop(3, "河內老城區", new TimeOnly(15, 0), "3 小時", new TimeOnly(18, 0), Photo(i++, "河內老城區"),
                    Description: "36 條老街各自賣不同商品，很好逛。"),
                new SeedStop(4, "文廟", new TimeOnly(9, 0), "1.5 小時", new TimeOnly(10, 30), Photo(i++, "文廟")),
                new SeedStop(4, "胡志明陵寢", new TimeOnly(11, 0), "1 小時", new TimeOnly(12, 0), Photo(i++, "胡志明陵寢")),
                new SeedStop(5, "河內機場伴手禮", new TimeOnly(10, 0), "1.5 小時", new TimeOnly(11, 30), Photo(i++, "河內機場伴手禮")),
                new SeedStop(5, "36 古街咖啡巡禮", new TimeOnly(8, 0), "1.5 小時", new TimeOnly(9, 30), Photo(i++, "36 古街咖啡巡禮")),
            });

        yield return new SeedPost(2, "上海外灘夜景兩天一夜", Photo(i++, "上海外灘夜景兩天一夜"), VlogMediaType.Photo,
            "東方明珠配黃浦江夜景，短短兩天的城市小旅行。",
            "上海", 2, new DateOnly(2026, 10, 1), VlogPostStatus.Published, TravelGroupSize.Solo,
            new[]
            {
                new SeedStop(1, "外灘", new TimeOnly(19, 0), "1.5 小時", new TimeOnly(20, 30), Photo(i++, "外灘")),
                new SeedStop(1, "南京東路步行街", new TimeOnly(15, 0), "2 小時", new TimeOnly(17, 0), Photo(i++, "南京東路步行街")),
                new SeedStop(2, "豫園", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "豫園"),
                    Description: "江南古典園林，周邊小吃街也很熱鬧。"),
                new SeedStop(2, "東方明珠塔", new TimeOnly(13, 0), "1.5 小時", new TimeOnly(14, 30), Photo(i++, "東方明珠塔")),
            });

        yield return new SeedPost(3, "香港迪士尼樂園親子遊 3 天 2 夜", Photo(i++, "香港迪士尼樂園親子遊 3 天 2 夜"), VlogMediaType.Photo,
            "帶小孩去迪士尼，晚上煙火秀是整趟旅程的重頭戲。",
            "香港", 3, new DateOnly(2026, 12, 20), VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "香港迪士尼樂園", new TimeOnly(10, 0), "8 小時", new TimeOnly(18, 0), Photo(i++, "香港迪士尼樂園")),
                new SeedStop(1, "迪士尼小鎮商店街", new TimeOnly(18, 30), "1 小時", new TimeOnly(19, 30), Photo(i++, "迪士尼小鎮商店街")),
                new SeedStop(2, "太平山頂", new TimeOnly(17, 0), "1.5 小時", new TimeOnly(18, 30), Photo(i++, "太平山頂"),
                    Description: "俯瞰維多利亞港夜景的經典角度。"),
                new SeedStop(2, "中環蘇豪區", new TimeOnly(11, 0), "2 小時", new TimeOnly(13, 0), Photo(i++, "中環蘇豪區")),
                new SeedStop(3, "尖沙咀星光大道", new TimeOnly(18, 0), "1.5 小時", new TimeOnly(19, 30), Photo(i++, "尖沙咀星光大道"),
                    Description: "晚上有幻彩詠香江燈光秀。"),
                new SeedStop(3, "旺角女人街", new TimeOnly(14, 0), "2 小時", new TimeOnly(16, 0), Photo(i++, "旺角女人街")),
            });

        yield return new SeedPost(1, "澳門大三巴牌坊美食一日遊", Photo(i++, "澳門大三巴牌坊美食一日遊"), VlogMediaType.Photo,
            "從葡式蛋塔吃到水蟹粥，一天走完舊城區精華。",
            "澳門", 1, null, VlogPostStatus.Draft, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "大三巴牌坊", new TimeOnly(14, 0), "1 小時", new TimeOnly(15, 0), Photo(i++, "大三巴牌坊")),
                new SeedStop(1, "議事亭前地", new TimeOnly(15, 30), "1 小時", new TimeOnly(16, 30), Photo(i++, "議事亭前地"),
                    Description: "葡式碎石地磚廣場，周邊蛋塔店密集。"),
            });

        yield return new SeedPost(2, "紐約自由女神像跨年 6 天 5 夜", Photo(i++, "紐約自由女神像跨年 6 天 5 夜"), VlogMediaType.Photo,
            "第一次在時代廣場跨年，人多但氣氛真的很值得體驗一次。",
            "紐約", 6, new DateOnly(2025, 12, 30), VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "自由女神像", new TimeOnly(10, 0), "3 小時", new TimeOnly(13, 0), Photo(i++, "自由女神像"),
                    Remarks: "建議提前上官網買船票，現場排隊常常要等很久。"),
                new SeedStop(1, "華爾街銅牛", new TimeOnly(14, 0), "1 小時", new TimeOnly(15, 0), Photo(i++, "華爾街銅牛")),
                new SeedStop(2, "中央公園", new TimeOnly(10, 0), "2.5 小時", new TimeOnly(12, 30), Photo(i++, "中央公園")),
                new SeedStop(2, "自然歷史博物館", new TimeOnly(13, 0), "2.5 小時", new TimeOnly(15, 30), Photo(i++, "自然歷史博物館")),
                new SeedStop(3, "大都會博物館", new TimeOnly(10, 0), "3 小時", new TimeOnly(13, 0), Photo(i++, "大都會博物館")),
                new SeedStop(3, "洛克斐勒中心觀景台", new TimeOnly(17, 0), "1.5 小時", new TimeOnly(18, 30), Photo(i++, "洛克斐勒中心觀景台")),
                new SeedStop(4, "第五大道購物", new TimeOnly(13, 0), "3 小時", new TimeOnly(16, 0), Photo(i++, "第五大道購物")),
                new SeedStop(4, "百老匯音樂劇", new TimeOnly(19, 0), "2.5 小時", new TimeOnly(21, 30), Photo(i++, "百老匯音樂劇")),
                new SeedStop(5, "時代廣場跨年倒數", new TimeOnly(20, 0), "5 小時", new TimeOnly(1, 0), Photo(i++, "時代廣場跨年倒數")),
                new SeedStop(5, "跨年前逛街暖身", new TimeOnly(14, 0), "3 小時", new TimeOnly(17, 0), Photo(i++, "跨年前逛街暖身")),
                new SeedStop(6, "布魯克林大橋", new TimeOnly(10, 0), "1.5 小時", new TimeOnly(11, 30), Photo(i++, "布魯克林大橋")),
                new SeedStop(6, "布魯克林高地眺望台", new TimeOnly(12, 0), "1 小時", new TimeOnly(13, 0), Photo(i++, "布魯克林高地眺望台")),
            });

        yield return new SeedPost(4, "【官方精選】巴黎艾菲爾鐵塔浪漫之旅 7 天 6 夜", Photo(i++, "【官方精選】巴黎艾菲爾鐵塔浪漫之旅 7 天 6 夜"), VlogMediaType.Photo,
            "LazyTravel 團隊整理的巴黎經典路線，適合第一次去歐洲的旅人。",
            "巴黎", 7, null, VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "艾菲爾鐵塔", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "艾菲爾鐵塔")),
                new SeedStop(1, "戰神廣場野餐", new TimeOnly(12, 30), "1.5 小時", new TimeOnly(14, 0), Photo(i++, "戰神廣場野餐")),
                new SeedStop(2, "羅浮宮", new TimeOnly(9, 0), "4 小時", new TimeOnly(13, 0), Photo(i++, "羅浮宮"),
                    Remarks: "館藏眾多，建議先規劃想看的重點展區。"),
                new SeedStop(2, "杜樂麗花園", new TimeOnly(14, 0), "1.5 小時", new TimeOnly(15, 30), Photo(i++, "杜樂麗花園")),
                new SeedStop(3, "凱旋門與香榭麗舍大道", new TimeOnly(14, 0), "2 小時", new TimeOnly(16, 0), Photo(i++, "凱旋門與香榭麗舍大道")),
                new SeedStop(3, "協和廣場", new TimeOnly(16, 30), "1 小時", new TimeOnly(17, 30), Photo(i++, "協和廣場")),
                new SeedStop(4, "凡爾賽宮一日遊", new TimeOnly(9, 0), "7 小時", new TimeOnly(16, 0), Photo(i++, "凡爾賽宮一日遊")),
                new SeedStop(4, "凡爾賽小鎮晚餐", new TimeOnly(17, 0), "1.5 小時", new TimeOnly(18, 30), Photo(i++, "凡爾賽小鎮晚餐")),
                new SeedStop(5, "蒙馬特高地", new TimeOnly(10, 0), "2.5 小時", new TimeOnly(12, 30), Photo(i++, "蒙馬特高地"),
                    Description: "聖心堂加畫家村，適合悠閒散步。"),
                new SeedStop(5, "紅磨坊周邊晚餐", new TimeOnly(19, 0), "2 小時", new TimeOnly(21, 0), Photo(i++, "紅磨坊周邊晚餐")),
                new SeedStop(6, "塞納河遊船", new TimeOnly(19, 0), "1.5 小時", new TimeOnly(20, 30), Photo(i++, "塞納河遊船")),
                new SeedStop(6, "西堤島聖母院外觀", new TimeOnly(10, 0), "1 小時", new TimeOnly(11, 0), Photo(i++, "西堤島聖母院外觀")),
                new SeedStop(7, "老佛爺百貨伴手禮", new TimeOnly(10, 0), "3 小時", new TimeOnly(13, 0), Photo(i++, "老佛爺百貨伴手禮")),
                new SeedStop(7, "歌劇院周邊街景", new TimeOnly(13, 30), "1 小時", new TimeOnly(14, 30), Photo(i++, "歌劇院周邊街景")),
            });

        yield return new SeedPost(1, "倫敦大笨鐘皇家巡禮 6 天 5 夜", Photo(i++, "倫敦大笨鐘皇家巡禮 6 天 5 夜"), VlogMediaType.Photo,
            "看了衛兵交接儀式，順便安排一天去溫莎城堡。",
            "倫敦", 6, new DateOnly(2026, 5, 22), VlogPostStatus.Published, TravelGroupSize.Solo,
            new[]
            {
                new SeedStop(1, "大笨鐘與國會大廈", new TimeOnly(11, 0), "1 小時", new TimeOnly(12, 0), Photo(i++, "大笨鐘與國會大廈")),
                new SeedStop(1, "白金漢宮衛兵交接", new TimeOnly(11, 30), "1 小時", new TimeOnly(12, 30), Photo(i++, "白金漢宮衛兵交接"),
                    Remarks: "交接儀式並非每天舉行，出發前先查官網時間。"),
                new SeedStop(2, "大英博物館", new TimeOnly(10, 0), "3 小時", new TimeOnly(13, 0), Photo(i++, "大英博物館")),
                new SeedStop(2, "柯芬園晚間市集", new TimeOnly(18, 0), "1.5 小時", new TimeOnly(19, 30), Photo(i++, "柯芬園晚間市集")),
                new SeedStop(3, "倫敦塔橋", new TimeOnly(14, 0), "1.5 小時", new TimeOnly(15, 30), Photo(i++, "倫敦塔橋")),
                new SeedStop(3, "倫敦眼", new TimeOnly(17, 0), "1 小時", new TimeOnly(18, 0), Photo(i++, "倫敦眼")),
                new SeedStop(4, "溫莎城堡一日遊", new TimeOnly(9, 0), "6 小時", new TimeOnly(15, 0), Photo(i++, "溫莎城堡一日遊")),
                new SeedStop(4, "伊頓公學外觀漫步", new TimeOnly(15, 30), "1 小時", new TimeOnly(16, 30), Photo(i++, "伊頓公學外觀漫步")),
                new SeedStop(5, "柯芬園", new TimeOnly(11, 0), "2 小時", new TimeOnly(13, 0), Photo(i++, "柯芬園"),
                    Description: "街頭藝人表演跟市集很熱鬧。"),
                new SeedStop(5, "大英圖書館", new TimeOnly(14, 0), "1.5 小時", new TimeOnly(15, 30), Photo(i++, "大英圖書館")),
                new SeedStop(6, "波特貝羅市集", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "波特貝羅市集"),
                    Remarks: "只有週六市集規模最大，記得挑對日子。"),
                new SeedStop(6, "諾丁丘街景", new TimeOnly(12, 30), "1.5 小時", new TimeOnly(14, 0), Photo(i++, "諾丁丘街景")),
            });

        yield return new SeedPost(2, "羅馬競技場古蹟巡禮 6 天 5 夜", Photo(i++, "羅馬競技場古蹟巡禮 6 天 5 夜"), VlogMediaType.Photo,
            "走一趟古羅馬遺跡，順便安排了一天去梵蒂岡博物館。",
            "羅馬", 6, null, VlogPostStatus.Draft, TravelGroupSize.Solo,
            new[]
            {
                new SeedStop(1, "羅馬競技場", new TimeOnly(9, 0), "2.5 小時", new TimeOnly(11, 30), Photo(i++, "羅馬競技場"),
                    Remarks: "旺季建議提前預約門票，現場排隊動輒一兩小時。"),
                new SeedStop(1, "古羅馬廣場", new TimeOnly(12, 0), "1.5 小時", new TimeOnly(13, 30), Photo(i++, "古羅馬廣場")),
                new SeedStop(2, "梵蒂岡博物館", new TimeOnly(9, 0), "4 小時", new TimeOnly(13, 0), Photo(i++, "梵蒂岡博物館")),
                new SeedStop(2, "聖彼得大教堂", new TimeOnly(13, 30), "1.5 小時", new TimeOnly(15, 0), Photo(i++, "聖彼得大教堂")),
                new SeedStop(3, "許願池", new TimeOnly(16, 0), "1 小時", new TimeOnly(17, 0), Photo(i++, "許願池")),
                new SeedStop(3, "波各賽別墅公園", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "波各賽別墅公園")),
                new SeedStop(4, "萬神殿", new TimeOnly(10, 0), "1 小時", new TimeOnly(11, 0), Photo(i++, "萬神殿")),
                new SeedStop(4, "納沃納廣場", new TimeOnly(11, 30), "1.5 小時", new TimeOnly(13, 0), Photo(i++, "納沃納廣場")),
                new SeedStop(5, "西班牙階梯", new TimeOnly(15, 0), "1 小時", new TimeOnly(16, 0), Photo(i++, "西班牙階梯")),
                new SeedStop(5, "人民廣場", new TimeOnly(16, 30), "1 小時", new TimeOnly(17, 30), Photo(i++, "人民廣場")),
                new SeedStop(6, "越台伯河區晚餐", new TimeOnly(19, 0), "2 小時", new TimeOnly(21, 0), Photo(i++, "越台伯河區晚餐"),
                    Description: "在地人推薦的餐廳巷弄，觀光客較少。"),
                new SeedStop(6, "市區伴手禮採買", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "市區伴手禮採買")),
            });

        yield return new SeedPost(3, "巴塞隆納聖家堂建築之旅 6 天 5 夜", Photo(i++, "巴塞隆納聖家堂建築之旅 6 天 5 夜"), VlogMediaType.Photo,
            "高第建築控必訪清單，聖家堂內部的光影效果很震撼。",
            "巴塞隆納", 6, new DateOnly(2026, 9, 14), VlogPostStatus.Published, TravelGroupSize.Solo,
            new[]
            {
                new SeedStop(1, "聖家堂", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "聖家堂"),
                    Description: "彩色玻璃在中午光線最美，建議中午前後入場。"),
                new SeedStop(1, "格拉西亞大道", new TimeOnly(15, 0), "2 小時", new TimeOnly(17, 0), Photo(i++, "格拉西亞大道")),
                new SeedStop(2, "奎爾公園", new TimeOnly(9, 0), "1.5 小時", new TimeOnly(10, 30), Photo(i++, "奎爾公園")),
                new SeedStop(2, "巴塞隆納主教座堂", new TimeOnly(11, 0), "1 小時", new TimeOnly(12, 0), Photo(i++, "巴塞隆納主教座堂")),
                new SeedStop(3, "哥德區", new TimeOnly(14, 0), "2 小時", new TimeOnly(16, 0), Photo(i++, "哥德區")),
                new SeedStop(3, "畢卡索美術館", new TimeOnly(16, 30), "1.5 小時", new TimeOnly(18, 0), Photo(i++, "畢卡索美術館")),
                new SeedStop(4, "巴特婁之家", new TimeOnly(10, 0), "1.5 小時", new TimeOnly(11, 30), Photo(i++, "巴特婁之家")),
                new SeedStop(4, "米拉之家", new TimeOnly(12, 0), "1 小時", new TimeOnly(13, 0), Photo(i++, "米拉之家")),
                new SeedStop(5, "蘭布拉大道", new TimeOnly(17, 0), "2 小時", new TimeOnly(19, 0), Photo(i++, "蘭布拉大道"),
                    Description: "波格利亞市場也在附近，可以順路逛。"),
                new SeedStop(5, "巴塞隆納海灘", new TimeOnly(11, 0), "2 小時", new TimeOnly(13, 0), Photo(i++, "巴塞隆納海灘")),
                new SeedStop(6, "蒙特惠奇山纜車", new TimeOnly(15, 0), "2 小時", new TimeOnly(17, 0), Photo(i++, "蒙特惠奇山纜車")),
                new SeedStop(6, "西班牙廣場魔幻噴泉", new TimeOnly(20, 0), "1.5 小時", new TimeOnly(21, 30), Photo(i++, "西班牙廣場魔幻噴泉")),
            });

        yield return new SeedPost(1, "雪梨歌劇院跨年煙火 5 天 4 夜", Photo(i++, "雪梨歌劇院跨年煙火 5 天 4 夜"), VlogMediaType.Photo,
            "在海港大橋旁邊卡位看跨年煙火，畫面終生難忘。",
            "雪梨", 5, new DateOnly(2025, 12, 31), VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "雪梨歌劇院", new TimeOnly(15, 0), "1.5 小時", new TimeOnly(16, 30), Photo(i++, "雪梨歌劇院")),
                new SeedStop(1, "海港大橋跨年煙火", new TimeOnly(21, 0), "4 小時", new TimeOnly(1, 0), Photo(i++, "海港大橋跨年煙火"),
                    Remarks: "建議下午就先去卡位，晚點會完全找不到位置。"),
                new SeedStop(2, "邦迪海灘", new TimeOnly(10, 0), "2.5 小時", new TimeOnly(12, 30), Photo(i++, "邦迪海灘")),
                new SeedStop(2, "邦迪至庫吉海岸步道", new TimeOnly(13, 0), "2 小時", new TimeOnly(15, 0), Photo(i++, "邦迪至庫吉海岸步道")),
                new SeedStop(3, "藍山國家公園一日遊", new TimeOnly(8, 0), "8 小時", new TimeOnly(16, 0), Photo(i++, "藍山國家公園一日遊"),
                    Description: "三姊妹岩跟纜車景觀是必看重點。"),
                new SeedStop(3, "魯拉小鎮咖啡廳", new TimeOnly(16, 30), "1 小時", new TimeOnly(17, 30), Photo(i++, "魯拉小鎮咖啡廳")),
                new SeedStop(4, "岩石區市集", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "岩石區市集")),
                new SeedStop(4, "雪梨魚市場", new TimeOnly(12, 30), "1.5 小時", new TimeOnly(14, 0), Photo(i++, "雪梨魚市場")),
                new SeedStop(5, "達令港", new TimeOnly(17, 0), "2 小時", new TimeOnly(19, 0), Photo(i++, "達令港")),
                new SeedStop(5, "雪梨塔觀景台", new TimeOnly(11, 0), "1.5 小時", new TimeOnly(12, 30), Photo(i++, "雪梨塔觀景台")),
            });

        yield return new SeedPost(2, "紐西蘭皇后鎮極限運動 8 天 7 夜", Photo(i++, "紐西蘭皇后鎮極限運動 8 天 7 夜"), VlogMediaType.Photo,
            "高空彈跳挑戰成功，順便安排了米佛峽灣一日遊。",
            "皇后鎮", 8, null, VlogPostStatus.Draft, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "卡瓦拉大橋高空彈跳", new TimeOnly(10, 0), "1 小時", new TimeOnly(11, 0), Photo(i++, "卡瓦拉大橋高空彈跳"),
                    Remarks: "需事先線上預約，旺季名額很快額滿。"),
                new SeedStop(1, "皇后鎮湖濱步道", new TimeOnly(13, 0), "1.5 小時", new TimeOnly(14, 30), Photo(i++, "皇后鎮湖濱步道")),
                new SeedStop(2, "米佛峽灣一日遊", new TimeOnly(7, 0), "10 小時", new TimeOnly(17, 0), Photo(i++, "米佛峽灣一日遊"),
                    Description: "被稱為世界第八大奇景，來回車程較長。"),
                new SeedStop(2, "蒂阿瑙湖畔晚餐", new TimeOnly(18, 0), "1.5 小時", new TimeOnly(19, 30), Photo(i++, "蒂阿瑙湖畔晚餐")),
                new SeedStop(3, "格林諾奇", new TimeOnly(10, 0), "3 小時", new TimeOnly(13, 0), Photo(i++, "格林諾奇"),
                    Description: "魔戒取景地，湖光山色很療癒。"),
                new SeedStop(3, "TSS 蒸汽船遊湖", new TimeOnly(14, 0), "3 小時", new TimeOnly(17, 0), Photo(i++, "TSS 蒸汽船遊湖")),
                new SeedStop(4, "皇后鎮花園", new TimeOnly(9, 0), "1.5 小時", new TimeOnly(10, 30), Photo(i++, "皇后鎮花園")),
                new SeedStop(4, "瓦卡蒂普湖畔咖啡廳", new TimeOnly(11, 0), "1.5 小時", new TimeOnly(12, 30), Photo(i++, "瓦卡蒂普湖畔咖啡廳")),
                new SeedStop(5, "箭鎮", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "箭鎮"),
                    Description: "淘金小鎮，秋天楓紅時特別美。"),
                new SeedStop(5, "箭鎮淘金體驗", new TimeOnly(12, 30), "1 小時", new TimeOnly(13, 30), Photo(i++, "箭鎮淘金體驗")),
                new SeedStop(6, "瓦納卡湖", new TimeOnly(11, 0), "2 小時", new TimeOnly(13, 0), Photo(i++, "瓦納卡湖")),
                new SeedStop(6, "孤獨樹拍照點", new TimeOnly(13, 30), "1 小時", new TimeOnly(14, 30), Photo(i++, "孤獨樹拍照點")),
                new SeedStop(7, "皇后鎮天空纜車", new TimeOnly(17, 0), "1.5 小時", new TimeOnly(18, 30), Photo(i++, "皇后鎮天空纜車")),
                new SeedStop(7, "山頂雪橇滑道", new TimeOnly(15, 0), "1.5 小時", new TimeOnly(16, 30), Photo(i++, "山頂雪橇滑道")),
                new SeedStop(8, "機場免稅店", new TimeOnly(9, 0), "1.5 小時", new TimeOnly(10, 30), Photo(i++, "機場免稅店")),
                new SeedStop(8, "市區最後採買", new TimeOnly(11, 0), "1.5 小時", new TimeOnly(12, 30), Photo(i++, "市區最後採買")),
            });

        yield return new SeedPost(3, "溫哥華史丹利公園賞楓 5 天 4 夜", Photo(i++, "溫哥華史丹利公園賞楓 5 天 4 夜"), VlogMediaType.Photo,
            "秋天的史丹利公園楓紅超美，順便租了單車環公園一圈。",
            "溫哥華", 5, new DateOnly(2026, 10, 12), VlogPostStatus.Published, TravelGroupSize.Solo,
            new[]
            {
                new SeedStop(1, "史丹利公園", new TimeOnly(10, 0), "2.5 小時", new TimeOnly(12, 30), Photo(i++, "史丹利公園")),
                new SeedStop(1, "溫哥華水族館", new TimeOnly(13, 0), "1.5 小時", new TimeOnly(14, 30), Photo(i++, "溫哥華水族館")),
                new SeedStop(2, "格蘭維爾島市場", new TimeOnly(11, 0), "2 小時", new TimeOnly(13, 0), Photo(i++, "格蘭維爾島市場")),
                new SeedStop(2, "耶魯鎮", new TimeOnly(14, 0), "1.5 小時", new TimeOnly(15, 30), Photo(i++, "耶魯鎮")),
                new SeedStop(3, "卡皮拉諾吊橋公園", new TimeOnly(10, 0), "2.5 小時", new TimeOnly(12, 30), Photo(i++, "卡皮拉諾吊橋公園"),
                    Description: "高空吊橋加樹頂步道，適合喜歡刺激的人。"),
                new SeedStop(3, "格勞斯山纜車", new TimeOnly(13, 30), "2 小時", new TimeOnly(15, 30), Photo(i++, "格勞斯山纜車")),
                new SeedStop(4, "煤氣鎮", new TimeOnly(14, 0), "1.5 小時", new TimeOnly(15, 30), Photo(i++, "煤氣鎮")),
                new SeedStop(4, "溫哥華美術館", new TimeOnly(11, 0), "2 小時", new TimeOnly(13, 0), Photo(i++, "溫哥華美術館")),
                new SeedStop(5, "惠斯勒一日遊", new TimeOnly(8, 0), "9 小時", new TimeOnly(17, 0), Photo(i++, "惠斯勒一日遊"),
                    Remarks: "冬奧滑雪勝地，秋天適合纜車賞景。"),
                new SeedStop(5, "機場周邊伴手禮採買", new TimeOnly(17, 30), "1.5 小時", new TimeOnly(19, 0), Photo(i++, "機場周邊伴手禮採買")),
            });

        yield return new SeedPost(1, "夏威夷歐胡島衝浪度假 5 天 4 夜", Photo(i++, "夏威夷歐胡島衝浪度假 5 天 4 夜"), VlogMediaType.Photo,
            "第一次上衝浪課就成功站起來，威基基海灘的夕陽也很值得。",
            "夏威夷", 5, null, VlogPostStatus.Draft, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "威基基海灘", new TimeOnly(16, 0), "2 小時", new TimeOnly(18, 0), Photo(i++, "威基基海灘")),
                new SeedStop(1, "威基基衝浪課程", new TimeOnly(9, 0), "2 小時", new TimeOnly(11, 0), Photo(i++, "威基基衝浪課程")),
                new SeedStop(2, "鑽石頭山健行", new TimeOnly(7, 0), "2 小時", new TimeOnly(9, 0), Photo(i++, "鑽石頭山健行"),
                    Remarks: "建議一早去，避開中午的大太陽。"),
                new SeedStop(2, "恐龍灣浮潛", new TimeOnly(10, 0), "2.5 小時", new TimeOnly(12, 30), Photo(i++, "恐龍灣浮潛")),
                new SeedStop(3, "北岸衝浪聖地", new TimeOnly(9, 0), "4 小時", new TimeOnly(13, 0), Photo(i++, "北岸衝浪聖地"),
                    Description: "冬天浪況大，適合看職業選手比賽。"),
                new SeedStop(3, "北岸蝦車小吃", new TimeOnly(13, 30), "1 小時", new TimeOnly(14, 30), Photo(i++, "北岸蝦車小吃")),
                new SeedStop(4, "珍珠港歷史園區", new TimeOnly(9, 0), "3 小時", new TimeOnly(12, 0), Photo(i++, "珍珠港歷史園區")),
                new SeedStop(4, "玻里尼西亞文化中心", new TimeOnly(14, 0), "3 小時", new TimeOnly(17, 0), Photo(i++, "玻里尼西亞文化中心")),
                new SeedStop(5, "阿拉莫阿納購物中心", new TimeOnly(11, 0), "3 小時", new TimeOnly(14, 0), Photo(i++, "阿拉莫阿納購物中心")),
                new SeedStop(5, "威基基機場周邊伴手禮", new TimeOnly(9, 0), "1.5 小時", new TimeOnly(10, 30), Photo(i++, "威基基機場周邊伴手禮")),
            });

        yield return new SeedPost(2, "關島跳島潛水 4 天 3 夜", Photo(i++, "關島跳島潛水 4 天 3 夜"), VlogMediaType.Photo,
            "水肺潛水體驗證入門首選，能見度非常好。",
            "關島", 4, new DateOnly(2026, 6, 30), VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "戀人岬", new TimeOnly(9, 0), "1 小時", new TimeOnly(10, 0), Photo(i++, "戀人岬")),
                new SeedStop(1, "杜夢灣沙灘散步", new TimeOnly(17, 0), "1 小時", new TimeOnly(18, 0), Photo(i++, "杜夢灣沙灘散步")),
                new SeedStop(2, "杜夢灣潛水點", new TimeOnly(9, 0), "2 小時", new TimeOnly(11, 0), Photo(i++, "杜夢灣潛水點"),
                    Description: "珊瑚礁保存完整，很適合新手體驗潛水。"),
                new SeedStop(2, "水底世界水族館", new TimeOnly(13, 0), "1.5 小時", new TimeOnly(14, 30), Photo(i++, "水底世界水族館")),
                new SeedStop(3, "情人崖", new TimeOnly(15, 0), "1 小時", new TimeOnly(16, 0), Photo(i++, "情人崖")),
                new SeedStop(3, "南部島嶼一日遊", new TimeOnly(9, 0), "5 小時", new TimeOnly(14, 0), Photo(i++, "南部島嶼一日遊")),
                new SeedStop(4, "查莫洛夜市", new TimeOnly(18, 0), "2 小時", new TimeOnly(20, 0), Photo(i++, "查莫洛夜市"),
                    Description: "只有週三晚上開放，有傳統歌舞表演。"),
                new SeedStop(4, "GPO 購物中心", new TimeOnly(11, 0), "2 小時", new TimeOnly(13, 0), Photo(i++, "GPO 購物中心")),
            });

        yield return new SeedPost(3, "峇里島金巴蘭海灘日落晚餐 5 天 4 夜", Photo(i++, "峇里島金巴蘭海灘日落晚餐 5 天 4 夜"), VlogMediaType.Photo,
            "沙灘上吃海鮮大餐配日落，整趟旅程最浪漫的一晚。",
            "峇里島", 5, new DateOnly(2026, 8, 8), VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "烏布皇宮", new TimeOnly(9, 0), "1.5 小時", new TimeOnly(10, 30), Photo(i++, "烏布皇宮")),
                new SeedStop(1, "烏布傳統市場", new TimeOnly(11, 0), "1.5 小時", new TimeOnly(12, 30), Photo(i++, "烏布傳統市場")),
                new SeedStop(2, "金巴蘭海灘", new TimeOnly(17, 30), "2 小時", new TimeOnly(19, 30), Photo(i++, "金巴蘭海灘"),
                    Description: "沙灘海鮮餐廳眾多，建議提前訂位看日落。"),
                new SeedStop(2, "烏魯瓦圖斷崖廟", new TimeOnly(15, 0), "1.5 小時", new TimeOnly(16, 30), Photo(i++, "烏魯瓦圖斷崖廟"),
                    Description: "傍晚有傳統克差舞表演。"),
                new SeedStop(3, "德格拉朗梯田", new TimeOnly(9, 0), "2 小時", new TimeOnly(11, 0), Photo(i++, "德格拉朗梯田")),
                new SeedStop(3, "聖泉廟", new TimeOnly(13, 0), "1.5 小時", new TimeOnly(14, 30), Photo(i++, "聖泉廟")),
                new SeedStop(4, "海神廟", new TimeOnly(16, 0), "1.5 小時", new TimeOnly(17, 30), Photo(i++, "海神廟"),
                    Description: "退潮才能走到廟前岩礁，日落景色很美。"),
                new SeedStop(4, "水明漾衝浪灘", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "水明漾衝浪灘")),
                new SeedStop(5, "水明漾海灘俱樂部", new TimeOnly(11, 0), "3 小時", new TimeOnly(14, 0), Photo(i++, "水明漾海灘俱樂部")),
                new SeedStop(5, "峇里島機場伴手禮", new TimeOnly(9, 0), "1.5 小時", new TimeOnly(10, 30), Photo(i++, "峇里島機場伴手禮")),
            });

        yield return new SeedPost(1, "西雅圖太空針塔城市小旅行 4 天 3 夜", Photo(i++, "西雅圖太空針塔城市小旅行 4 天 3 夜"), VlogMediaType.Photo,
            "咖啡之都名不虛傳，順便去派克市場買了不少紀念品。",
            "西雅圖", 4, null, VlogPostStatus.Draft, TravelGroupSize.Solo,
            new[]
            {
                new SeedStop(1, "太空針塔", new TimeOnly(11, 0), "1.5 小時", new TimeOnly(12, 30), Photo(i++, "太空針塔")),
                new SeedStop(1, "流行文化博物館", new TimeOnly(13, 0), "1.5 小時", new TimeOnly(14, 30), Photo(i++, "流行文化博物館")),
                new SeedStop(2, "派克市場", new TimeOnly(9, 0), "2 小時", new TimeOnly(11, 0), Photo(i++, "派克市場"),
                    Description: "第一家星巴克跟飛魚表演都在這裡。"),
                new SeedStop(2, "先驅廣場", new TimeOnly(11, 30), "1 小時", new TimeOnly(12, 30), Photo(i++, "先驅廣場")),
                new SeedStop(3, "奇胡利玻璃博物館", new TimeOnly(10, 0), "1.5 小時", new TimeOnly(11, 30), Photo(i++, "奇胡利玻璃博物館")),
                new SeedStop(3, "西雅圖中央圖書館", new TimeOnly(13, 0), "1 小時", new TimeOnly(14, 0), Photo(i++, "西雅圖中央圖書館")),
                new SeedStop(4, "西雅圖水族館", new TimeOnly(10, 0), "2 小時", new TimeOnly(12, 0), Photo(i++, "西雅圖水族館")),
                new SeedStop(4, "煤氣廠公園", new TimeOnly(14, 0), "1.5 小時", new TimeOnly(15, 30), Photo(i++, "煤氣廠公園")),
            });

        // 示範一篇已軟刪除的文章，讓「已刪除」篩選頁有資料可看
        yield return new SeedPost(3, "舊金山公路旅行（內容已過期）", Photo(i++, "舊金山公路旅行（內容已過期）"), VlogMediaType.Photo,
            "沿1號公路一路開到優勝美地，內容已過時，先下架更新。",
            "舊金山", 7, null, VlogPostStatus.Published, TravelGroupSize.Small,
            new[]
            {
                new SeedStop(1, "金門大橋", new TimeOnly(10, 0), "1 小時", new TimeOnly(11, 0), Photo(i++, "金門大橋")),
                new SeedStop(1, "雙峰山觀景台", new TimeOnly(14, 0), "1 小時", new TimeOnly(15, 0), Photo(i++, "雙峰山觀景台")),
                new SeedStop(2, "漁人碼頭", new TimeOnly(11, 0), "2 小時", new TimeOnly(13, 0), Photo(i++, "漁人碼頭")),
                new SeedStop(2, "39 號碼頭海獅觀賞", new TimeOnly(13, 30), "1 小時", new TimeOnly(14, 30), Photo(i++, "39 號碼頭海獅觀賞")),
                new SeedStop(3, "惡魔島", new TimeOnly(10, 0), "2.5 小時", new TimeOnly(12, 30), Photo(i++, "惡魔島")),
                new SeedStop(3, "藝術宮", new TimeOnly(15, 0), "1 小時", new TimeOnly(16, 0), Photo(i++, "藝術宮")),
                new SeedStop(4, "九曲花街", new TimeOnly(14, 0), "1 小時", new TimeOnly(15, 0), Photo(i++, "九曲花街")),
                new SeedStop(4, "卡斯楚區街景", new TimeOnly(15, 30), "1 小時", new TimeOnly(16, 30), Photo(i++, "卡斯楚區街景")),
                new SeedStop(5, "優勝美地國家公園", new TimeOnly(8, 0), "9 小時", new TimeOnly(17, 0), Photo(i++, "優勝美地國家公園")),
                new SeedStop(6, "紅木森林", new TimeOnly(9, 0), "4 小時", new TimeOnly(13, 0), Photo(i++, "紅木森林")),
                new SeedStop(6, "太平洋濱海1號公路觀景點", new TimeOnly(14, 0), "2 小時", new TimeOnly(16, 0), Photo(i++, "太平洋濱海1號公路觀景點")),
                new SeedStop(7, "納帕酒莊", new TimeOnly(11, 0), "4 小時", new TimeOnly(15, 0), Photo(i++, "納帕酒莊"),
                    Description: "內容已過期，這篇之後會重新整理更新。"),
                new SeedStop(7, "索諾瑪酒鄉小鎮", new TimeOnly(15, 30), "1.5 小時", new TimeOnly(17, 0), Photo(i++, "索諾瑪酒鄉小鎮")),
            },
            SoftDeleted: true);

        yield return new SeedPost(2, "峇里島蜜月行（重複發文，已刪除）", Photo(i++, "峇里島蜜月行（重複發文，已刪除）"), VlogMediaType.Photo,
            "不小心發了兩篇一樣的內容，這篇先刪掉留另一篇。",
            "峇里島", 5, null, VlogPostStatus.Draft, TravelGroupSize.Solo,
            Array.Empty<SeedStop>(),
            SoftDeleted: true);
    }
}
