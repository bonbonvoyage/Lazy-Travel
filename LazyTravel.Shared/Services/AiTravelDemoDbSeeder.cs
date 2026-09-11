using LazyTravel.Shared.Models.EfModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Shared.Services;

/// <summary>
/// 開發環境的 AI 媒合展示資料。只新增既有資料表的資料，不變更資料庫結構。
/// 每筆資料都以固定標題或 Email 檢查，因此重複啟動不會重複建立。
/// </summary>
public static class AiTravelDemoDbSeeder
{
    private const string HualienTitle = "花蓮三天兩夜海岸小旅行";
    private const string KentingTitle = "墾丁四日海景放鬆之旅";

    public static async Task SeedAsync(LazyTravelDBContext context)
    {
        var now = DateTime.Now;
        var members = new[]
        {
            await EnsureMemberAsync(context, "ai-demo-yu@lazytravel.local", "小予", new DateOnly(2003, 4, 18), now),
            await EnsureMemberAsync(context, "ai-demo-an@lazytravel.local", "安安", new DateOnly(2004, 9, 3), now),
            await EnsureMemberAsync(context, "ai-demo-lin@lazytravel.local", "林沐", new DateOnly(2002, 12, 22), now),
            await EnsureMemberAsync(context, "ai-demo-ting@lazytravel.local", "庭庭", new DateOnly(2000, 7, 9), now),
        };

        await EnsureGroupAsync(
            context,
            title: HualienTitle,
            description: "沿著花蓮海岸慢慢旅行，安排七星潭、清水斷崖與石梯坪，適合喜歡看海與自然景點的旅伴。",
            country: "台灣",
            region: "花蓮",
            daysFromNow: 21,
            days: 3,
            maxPeople: 6,
            members: members[..3],
            imageUrl: "https://images.unsplash.com/photo-1499678329028-101435549a4e?w=1200&q=80",
            tags: new[] { "海邊", "自然景點", "慢遊", "海岸活動" },
            itinerary: new[]
            {
                (1, "七星潭看海散步", "七星潭", "沿著礫石海灘散步，欣賞太平洋海景。"),
                (2, "清水斷崖海岸行程", "清水斷崖", "安排海岸景觀與輕鬆拍照時間。"),
                (3, "石梯坪水上活動", "石梯坪", "依天候安排潮間帶或水上體驗後返程。"),
            });

        await EnsureGroupAsync(
            context,
            title: KentingTitle,
            description: "墾丁四天放鬆旅行，包含白沙灣、海景咖啡廳、水上活動與關山夕陽。",
            country: "台灣",
            region: "屏東",
            daysFromNow: 35,
            days: 4,
            maxPeople: 5,
            members: new[] { members[0], members[3] },
            imageUrl: "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=1200&q=80",
            tags: new[] { "沙灘", "海景", "水上活動", "放鬆" },
            itinerary: new[]
            {
                (1, "白沙灣海灘", "白沙灣", "在沙灘散步並欣賞海景。"),
                (2, "墾丁水上活動", "南灣", "依個人意願選擇浮潛或其他水上體驗。"),
                (3, "海景咖啡與夕陽", "關山", "下午放慢步調，傍晚欣賞夕陽。"),
                (4, "恆春散步與回程", "恆春", "走訪老街並預留充裕回程時間。"),
            });
    }

    private static async Task<Member> EnsureMemberAsync(
        LazyTravelDBContext context,
        string email,
        string name,
        DateOnly birthDate,
        DateTime now)
    {
        var member = await context.Users.FirstOrDefaultAsync(m => m.Email == email);
        if (member is not null)
        {
            return member;
        }

        member = new Member
        {
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            Name = name,
            ContactBookVisibility = 0,
            IsPrivateAccount = false,
            BirthDate = birthDate,
            Gender = 2,
            Occupation = "旅行愛好者",
            Bio = "AI 旅程媒合開發環境展示帳號。",
            Status = 1,
            CreatedAt = now,
            IsDelete = false,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        };
        member.PasswordHash = new PasswordHasher<Member>().HashPassword(member, "Demo1234!");
        context.Users.Add(member);
        await context.SaveChangesAsync();
        return member;
    }

    private static async Task EnsureGroupAsync(
        LazyTravelDBContext context,
        string title,
        string description,
        string country,
        string region,
        int daysFromNow,
        int days,
        int maxPeople,
        IReadOnlyList<Member> members,
        string imageUrl,
        IReadOnlyList<string> tags,
        IReadOnlyList<(int Day, string Title, string Location, string Description)> itinerary)
    {
        if (await context.TravelGroups.AnyAsync(g => g.GroupTitle == title && !g.IsDelete))
        {
            return;
        }

        var now = DateTime.Now;
        var startDate = DateOnly.FromDateTime(now.AddDays(daysFromNow));
        var group = new TravelGroup
        {
            OwnerMemberId = members[0].Id,
            GroupTitle = title,
            Description = description,
            StartDate = startDate,
            EndDate = startDate.AddDays(days - 1),
            MinPeople = 2,
            MaxPeople = maxPeople,
            CurrentPeople = members.Count,
            JoinRule = 1,
            GroupStatus = 0,
            IsPublic = true,
            CreatedAt = now,
            UpdatedAt = now,
            IsDelete = false,
            ReviewStatus = TravelGroupReviewStatus.Normal,
            Country = country,
            Region = region,
            AccommType = "民宿",
            CanShareRoom = true,
            AccommNote = "依實際成員需求安排。",
        };
        context.TravelGroups.Add(group);
        await context.SaveChangesAsync();

        context.TravelGroupImages.Add(new TravelGroupImage
        {
            GroupId = group.GroupId,
            ImageUrl = imageUrl,
            ImageType = 0,
            AltText = title,
            SortOrder = 0,
            IsCover = true,
            IsDeleted = false,
            UploadedByMemberId = members[0].Id,
            CreatedAt = now,
            UpdatedAt = now,
        });

        context.TravelGroupTags.AddRange(tags.Select((tag, index) => new TravelGroupTag
        {
            GroupId = group.GroupId,
            TagName = tag,
            TagCategory = index == 3 ? "旅遊方式" : "旅行興趣",
            CreatedAt = now,
            IsDelete = false,
        }));

        context.TravelGroupItineraryItems.AddRange(itinerary.Select((item, index) => new TravelGroupItineraryItem
        {
            GroupId = group.GroupId,
            DayNumber = item.Day,
            SortOrder = index,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0),
            Title = item.Title,
            LocationName = item.Location,
            Description = item.Description,
            CreatedAt = now,
            UpdatedAt = now,
        }));

        context.TravelGroupBudgets.AddRange(
            new TravelGroupBudget { GroupId = group.GroupId, BudgetCategory = 0, BudgetName = "住宿", Amount = days == 3 ? 3200 : 4800, CurrencyCode = "TWD", IsRequired = true, SortOrder = 0, CreatedAt = now, UpdatedAt = now },
            new TravelGroupBudget { GroupId = group.GroupId, BudgetCategory = 1, BudgetName = "交通", Amount = days == 3 ? 2200 : 2600, CurrencyCode = "TWD", IsRequired = true, SortOrder = 1, CreatedAt = now, UpdatedAt = now },
            new TravelGroupBudget { GroupId = group.GroupId, BudgetCategory = 3, BudgetName = "景點與活動", Amount = days == 3 ? 1200 : 2400, CurrencyCode = "TWD", IsRequired = false, SortOrder = 2, CreatedAt = now, UpdatedAt = now });

        context.GroupMembers.AddRange(members.Select((member, index) => new GroupMember
        {
            GroupId = group.GroupId,
            MemberId = member.Id,
            MemberRole = index == 0 ? (byte)1 : (byte)0,
            JoinedAt = now,
            IsRemoved = false,
            CreatedAt = now,
        }));

        await context.SaveChangesAsync();
    }
}
