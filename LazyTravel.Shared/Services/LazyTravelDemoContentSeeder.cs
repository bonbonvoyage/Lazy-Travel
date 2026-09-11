using LazyTravel.Shared.Models.EfModels;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Shared.Services;

/// <summary>
/// 開發用展示資料：用現有會員輪流建立揪團與行程文章，補足首頁與詳細頁所需資料。
/// 不綁固定會員 ID，也不覆蓋真實資料；只會補足資料到 30 筆，並用標題清單避免重複新增。
/// </summary>
public static class LazyTravelDemoContentSeeder
{
    private const int TargetCount = 30;

    private sealed record DestinationSeed(string Title, string Country, string Region, int Days, int MinPeople, int MaxPeople, TravelGroupSize PeopleSize, string Cover, string[] Gallery, string[] Tags, string Tone);

    private static readonly DestinationSeed[] Seeds =
    {
        S("京都秋日散策：巷弄裡的老靈魂", "日本", "京都", 5, "https://images.unsplash.com/photo-1545569341-9eb8b30979d9?w=1200&q=80", new[]{"散步","古都","攝影"}, "慢步調、咖啡與老街"),
        S("首爾自由行：市場與漢江夜風", "韓國", "首爾", 4, "https://images.unsplash.com/photo-1538485399081-7c8edc31e6c4?w=1200&q=80", new[]{"美食","購物","夜景"}, "美食密度高、城市交通方便"),
        S("曼谷香氣小旅行：寺廟與河岸", "泰國", "曼谷", 4, "https://images.unsplash.com/photo-1508009603885-50cf7c579365?w=1200&q=80", new[]{"寺廟","夜市","按摩"}, "輕鬆、熱鬧、適合第一次出國"),
        S("清邁慢生活：山城咖啡與手作", "泰國", "清邁", 5, "https://images.unsplash.com/photo-1575994532957-773da2f5c1e9?w=1200&q=80", new[]{"咖啡","手作","山城"}, "留白多、適合放空"),
        S("河內舊城散步：法式街角與越式咖啡", "越南", "河內", 4, "https://images.unsplash.com/photo-1509030450996-dd1a26dda07a?w=1200&q=80", new[]{"咖啡","老城","街拍"}, "低預算、街景細節多"),
        S("峴港海岸假期：沙灘與會安燈籠", "越南", "峴港", 5, "https://images.unsplash.com/photo-1559592413-7cec4d0cae2b?w=1200&q=80", new[]{"海邊","古城","放鬆"}, "適合看海與慢慢吃"),
        S("台南小吃地圖：古都兩日慢遊", "台灣", "台南", 2, "https://images.unsplash.com/photo-1540187334920-54e87c2771c0?w=1200&q=80", new[]{"小吃","古蹟","週末"}, "短天數、吃很多"),
        S("花蓮山海線：太平洋與峽谷", "台灣", "花蓮", 3, "https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?w=1200&q=80", new[]{"山海","自然","拍照"}, "風景大、步調舒服"),
        S("沖繩藍色海岸：自駕與島時間", "日本", "沖繩", 5, "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=1200&q=80", new[]{"自駕","海島","親水"}, "有海、有夕陽、有慢活"),
        S("北海道雪景五日：溫泉與小樽運河", "日本", "北海道", 5, "https://images.unsplash.com/photo-1510798831971-661eb04b3739?w=1200&q=80", new[]{"雪景","溫泉","美食"}, "冬季限定、照片很好看"),
        S("東京巷弄選物：咖啡與美術館", "日本", "東京", 4, "https://images.unsplash.com/photo-1540959733332-eab4deabeeaf?w=1200&q=80", new[]{"選物","咖啡","美術館"}, "城市感、安排彈性大"),
        S("大阪環球與道頓堀：熱鬧關西團", "日本", "大阪", 4, "https://images.unsplash.com/photo-1590559899731-a382839e5549?w=1200&q=80", new[]{"主題樂園","美食","購物"}, "熱鬧、好逛、好吃"),
        S("釜山海雲台：海景咖啡與市場", "韓國", "釜山", 4, "https://images.unsplash.com/photo-1517154421773-0529f29ea451?w=1200&q=80", new[]{"海景","市場","咖啡"}, "城市與海很近"),
        S("新加坡城市散步：花園與夜景", "新加坡", "新加坡", 3, "https://images.unsplash.com/photo-1525625293386-3f8f99389edd?w=1200&q=80", new[]{"夜景","城市","花園"}, "乾淨好走、適合短旅行"),
        S("吉隆坡雙塔與馬六甲：南洋小旅行", "馬來西亞", "吉隆坡", 4, "https://images.unsplash.com/photo-1596422846543-75c6fc197f07?w=1200&q=80", new[]{"南洋","城市","文化"}, "多元文化、價格友善"),
        S("香港離島一日接一日：茶餐廳與山海", "香港", "香港", 3, "https://images.unsplash.com/photo-1536599018102-9f803c140fc1?w=1200&q=80", new[]{"離島","茶餐廳","山海"}, "短天數也能很豐富"),
        S("澳門老街與葡式甜點", "澳門", "澳門", 2, "https://images.unsplash.com/photo-1555952238-4df4f902293d?w=1200&q=80", new[]{"甜點","老街","週末"}, "小巧、好拍、好吃"),
        S("峇里島瑜伽與海岸", "印尼", "峇里島", 6, "https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=1200&q=80", new[]{"海島","瑜伽","放鬆"}, "很適合充電"),
        S("雪梨港灣與藍山週末", "澳洲", "雪梨", 5, "https://images.unsplash.com/photo-1506973035872-a4ec16b8e8d9?w=1200&q=80", new[]{"港灣","自然","城市"}, "城市與自然兼具"),
        S("紐西蘭南島公路旅行", "紐西蘭", "南島", 8, "https://images.unsplash.com/photo-1469521669194-babb45599def?w=1200&q=80", new[]{"自駕","自然","公路"}, "大片風景、行程彈性"),
        S("巴黎美術館與塞納河散步", "法國", "巴黎", 5, "https://images.unsplash.com/photo-1502602898657-3e91760cbb34?w=1200&q=80", new[]{"美術館","散步","咖啡"}, "浪漫但不趕行程"),
        S("羅馬古城與托斯卡尼小鎮", "義大利", "羅馬", 6, "https://images.unsplash.com/photo-1529260830199-42c24126f198?w=1200&q=80", new[]{"古城","小鎮","美食"}, "歷史感、餐桌時間多"),
        S("巴塞隆納建築巡禮", "西班牙", "巴塞隆納", 5, "https://images.unsplash.com/photo-1583422409516-2895a77efded?w=1200&q=80", new[]{"建築","海邊","市集"}, "色彩鮮明、城市好走"),
        S("倫敦市集與劇院夜", "英國", "倫敦", 5, "https://images.unsplash.com/photo-1513635269975-59663e0ac1ad?w=1200&q=80", new[]{"市集","劇院","散步"}, "文化行程很多"),
        S("布拉格童話城堡散策", "捷克", "布拉格", 4, "https://images.unsplash.com/photo-1519677100203-a0e668c92439?w=1200&q=80", new[]{"城堡","老城","夜景"}, "童話感、步行友善"),
        S("冰島極光與溫泉小隊", "冰島", "雷克雅維克", 7, "https://images.unsplash.com/photo-1483347756197-71ef80e95f73?w=1200&q=80", new[]{"極光","溫泉","自然"}, "需要配合天氣、體力中等"),
        S("溫哥華森林與海岸", "加拿大", "溫哥華", 5, "https://images.unsplash.com/photo-1500534314209-a25ddb2bd429?w=1200&q=80", new[]{"森林","海岸","城市"}, "自然與城市距離近"),
        S("紐約街區與博物館日", "美國", "紐約", 5, "https://images.unsplash.com/photo-1485871981521-5b1fd3805eee?w=1200&q=80", new[]{"博物館","街區","城市"}, "節奏快但選擇多"),
        S("舊金山海灣與公路景", "美國", "舊金山", 5, "https://images.unsplash.com/photo-1501594907352-04cda38ebc29?w=1200&q=80", new[]{"公路","海灣","城市"}, "適合喜歡風景與咖啡的人"),
        S("阿姆斯特丹運河與博物館", "荷蘭", "阿姆斯特丹", 4, "https://images.unsplash.com/photo-1512470876302-972faa2aa9a4?w=1200&q=80", new[]{"運河","博物館","單車"}, "平緩、適合散步與騎車"),
    };

    private static DestinationSeed S(string title, string country, string region, int days, string cover, string[] tags, string tone)
    {
        var gallery = new[]
        {
            cover,
            "https://images.unsplash.com/photo-1500534314209-a25ddb2bd429?w=900&q=80",
            "https://images.unsplash.com/photo-1519608487953-e999c86e7455?w=900&q=80",
            "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=900&q=80",
        };
        var peopleSize = days >= 7 ? TravelGroupSize.Large : days <= 2 ? TravelGroupSize.Solo : TravelGroupSize.Small;
        var min = peopleSize == TravelGroupSize.Large ? 3 : 2;
        var max = peopleSize == TravelGroupSize.Large ? 8 : 4 + days % 3;
        return new DestinationSeed(title, country, region, days, min, max, peopleSize, cover, gallery, tags, tone);
    }

    public static async Task SeedAsync(LazyTravelDBContext context)
    {
        var members = await context.Users.Where(m => !m.IsDelete).OrderBy(m => m.Id).Take(12).ToListAsync();
        if (members.Count == 0) return;
        await EnsureTravelGroupsAsync(context, members);
        await EnsureVlogPostsAsync(context, members);
    }

    private static async Task EnsureTravelGroupsAsync(LazyTravelDBContext context, IReadOnlyList<Member> members)
    {
        var seedTitles = Seeds.Select(s => s.Title).ToArray();
        var existing = (await context.TravelGroups.Where(g => !g.IsDelete && seedTitles.Contains(g.GroupTitle)).Select(g => g.GroupTitle).ToListAsync()).ToHashSet(StringComparer.Ordinal);
        if (existing.Count >= TargetCount) return;
        var now = DateTime.Now;

        for (var i = 0; i < Seeds.Length && existing.Count < TargetCount; i++)
        {
            var seed = Seeds[i];
            var title = seed.Title;
            if (existing.Contains(title)) continue;
            var owner = members[i % members.Count];
            var start = DateOnly.FromDateTime(now.Date.AddDays(21 + i * 4));
            var current = Math.Min(seed.MaxPeople, seed.MinPeople + i % Math.Max(1, seed.MaxPeople - seed.MinPeople + 1));
            var group = new TravelGroup
            {
                OwnerMemberId = owner.Id,
                GroupTitle = title,
                Description = $"{seed.Tone}。行程包含封面、相簿、每日行程、預算與成員資料，適合旅伴一起討論旅行節奏。",
                StartDate = start,
                EndDate = start.AddDays(seed.Days - 1),
                MinPeople = seed.MinPeople,
                MaxPeople = seed.MaxPeople,
                CurrentPeople = current,
                JoinRule = 1,
                GroupStatus = 0,
                IsPublic = true,
                CreatedAt = now.AddMinutes(-i * 17),
                UpdatedAt = now.AddMinutes(-i * 7),
                IsDelete = false,
                ReviewStatus = TravelGroupReviewStatus.Normal,
                Country = seed.Country,
                Region = seed.Region,
            };
            context.TravelGroups.Add(group);
            await context.SaveChangesAsync();
            AddGroupChildren(context, group, seed, members, owner.Id, current, now, i);
            existing.Add(title);
        }
        await context.SaveChangesAsync();
    }

    private static void AddGroupChildren(LazyTravelDBContext context, TravelGroup group, DestinationSeed seed, IReadOnlyList<Member> members, int ownerId, int current, DateTime now, int index)
    {
        for (var i = 0; i < seed.Gallery.Length; i++)
        {
            context.TravelGroupImages.Add(new TravelGroupImage { GroupId = group.GroupId, ImageUrl = seed.Gallery[i], ImageType = 0, AltText = group.GroupTitle, SortOrder = i, IsCover = i == 0, IsDeleted = false, UploadedByMemberId = ownerId, CreatedAt = now, UpdatedAt = now });
        }
        context.GroupMembers.Add(new GroupMember { GroupId = group.GroupId, MemberId = ownerId, MemberRole = 1, JoinedAt = now, IsRemoved = false, CreatedAt = now });
        var added = 1;
        foreach (var member in members.Where(m => m.Id != ownerId))
        {
            if (added >= current) break;
            context.GroupMembers.Add(new GroupMember { GroupId = group.GroupId, MemberId = member.Id, MemberRole = 0, JoinedAt = now.AddMinutes(added), IsRemoved = false, CreatedAt = now.AddMinutes(added) });
            added++;
        }
        for (var day = 1; day <= Math.Min(seed.Days, 4); day++)
        {
            context.TravelGroupItineraryItems.Add(new TravelGroupItineraryItem { GroupId = group.GroupId, DayNumber = day, SortOrder = day - 1, Title = DayTitle(day, seed), LocationName = seed.Region, Description = DayDescription(day, seed), CreatedAt = now, UpdatedAt = now });
        }
        context.TravelGroupBudgets.AddRange(
            new TravelGroupBudget { GroupId = group.GroupId, BudgetCategory = 0, BudgetName = "住宿", Amount = 3200 + index * 180, CurrencyCode = "TWD", IsRequired = true, SortOrder = 0, CreatedAt = now, UpdatedAt = now },
            new TravelGroupBudget { GroupId = group.GroupId, BudgetCategory = 1, BudgetName = "交通", Amount = 2600 + index * 120, CurrencyCode = "TWD", IsRequired = true, SortOrder = 1, CreatedAt = now, UpdatedAt = now },
            new TravelGroupBudget { GroupId = group.GroupId, BudgetCategory = 2, BudgetName = "餐飲", Amount = 1800 + index * 90, CurrencyCode = "TWD", IsRequired = true, SortOrder = 2, CreatedAt = now, UpdatedAt = now });
        foreach (var tag in seed.Tags)
        {
            context.TravelGroupTags.Add(new TravelGroupTag { GroupId = group.GroupId, TagName = tag, TagCategory = "風格", CreatedAt = now, IsDelete = false });
        }
    }

    private static async Task EnsureVlogPostsAsync(LazyTravelDBContext context, IReadOnlyList<Member> members)
    {
        var seedTitles = Seeds.Select(s => s.Title).ToArray();
        var existing = (await context.VlogPosts.Where(p => !p.IsDelete && seedTitles.Contains(p.Title)).Select(p => p.Title).ToListAsync()).ToHashSet(StringComparer.Ordinal);
        if (existing.Count >= TargetCount) return;
        var now = DateTime.Now;

        for (var i = 0; i < Seeds.Length && existing.Count < TargetCount; i++)
        {
            var seed = Seeds[(i * 7) % Seeds.Length];
            var title = seed.Title;
            if (existing.Contains(title)) continue;
            var author = members[(i + 2) % members.Count];
            var post = new VlogPost
            {
                MemberId = author.Id,
                Title = title,
                MediaUrl = seed.Cover,
                MediaType = VlogMediaType.Photo,
                Content = $"這趟{seed.Country}{seed.Region}旅行，我們把節奏放慢，讓每天都有一點空白可以停下來觀察。{seed.Tone}，很適合想用照片、散步與餐桌記錄城市的人。",
                Destination = seed.Country,
                TravelDays = seed.Days,
                Status = VlogPostStatus.Published,
                CreatedAt = now.AddDays(-i - 1),
                UpdatedAt = now.AddDays(-i),
                TravelDate = now.Date.AddDays(-20 - i * 4),
                TravelPeople = seed.PeopleSize,
                IsDelete = false,
            };
            context.VlogPosts.Add(post);
            await context.SaveChangesAsync();
            AddPostChildren(context, post, seed, author.Id, now);
            existing.Add(title);
        }
        await context.SaveChangesAsync();
    }

    private static void AddPostChildren(LazyTravelDBContext context, VlogPost post, DestinationSeed seed, int authorId, DateTime now)
    {
        for (var i = 0; i < seed.Gallery.Length; i++)
        {
            context.VlogPostImages.Add(new VlogPostImage { VlogPostId = post.PostId, ImageUrl = seed.Gallery[i], ImageType = 0, AltText = post.Title, SortOrder = i, IsCover = i == 0, IsDeleted = false, UploadedByMemberId = authorId, CreatedAt = now, UpdatedAt = now });
        }
        for (var day = 1; day <= Math.Min(post.TravelDays, 4); day++)
        {
            context.ItineraryNodes.Add(new ItineraryNode
            {
                PostId = post.PostId,
                DayNumber = day,
                LocationName = DayLocation(day, seed),
                ArrivalTime = null,
                DepartureTime = null,
                StayTime = null,
                MediaUrl = seed.Gallery[day % seed.Gallery.Length],
                MediaType = VlogMediaType.Photo,
                Description = DayDescription(day, seed),
                Remarks = day switch
                {
                    1 => "上午｜抵達與交通確認\n下午｜住宿周邊散步\n晚上｜在地晚餐",
                    2 => "上午｜主要景點\n下午｜街區自由探索\n晚上｜夜景或小酒館",
                    3 => "上午｜市場散步\n下午｜咖啡館與選物\n晚上｜整理照片與心得",
                    _ => "上午｜慢慢收尾\n下午｜回程準備\n晚上｜回顧旅程",
                },
            });
        }
        foreach (var tag in seed.Tags)
        {
            context.VlogPostTags.Add(new VlogPostTag { PostId = post.PostId, TagName = tag, TagCategory = "文章標籤", CreatedAt = now, IsDelete = false });
        }
    }

    private static string DayTitle(int day, DestinationSeed seed) => day switch
    {
        1 => $"抵達{seed.Region}・集合暖身",
        2 => $"{seed.Region}經典景點散步",
        3 => "自由探索與在地餐桌",
        _ => "慢行收尾與伴手禮時間",
    };

    private static string DayLocation(int day, DestinationSeed seed) => day switch
    {
        1 => $"{seed.Region}車站周邊",
        2 => $"{seed.Region}主要街區",
        3 => "在地市場與咖啡館",
        _ => "回程前自由散步",
    };

    private static string DayDescription(int day, DestinationSeed seed) => day switch
    {
        1 => $"團員集合後先確認交通與住宿，再用輕鬆散步熟悉{seed.Region}的街區。",
        2 => "安排主要景點與拍照時間，下午保留空白，可以依天氣調整路線。",
        3 => "上午走訪市場或自然景點，下午自由活動，晚上一起整理隔天行程。",
        _ => "收拾行李、補買伴手禮，最後一起回顧這趟旅程。",
    };
}



