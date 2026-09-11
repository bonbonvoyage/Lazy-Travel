using LazyTravel.Shared.Models.EfModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Shared.Services;

// 開發用示範資料：跟 VlogPostDbSeeder 同一個模式，只在 TravelGroups 資料表是空的時候塞資料
// （重開機/資料庫已有真實資料就不會重複塞，也不會覆蓋別人建立的真實揪團）。
// 目的：首頁「熱門行程」區塊在揪團功能還沒有真實使用者資料前，先有東西可以對照畫面效果；
// 之後有真實資料了，這批列可以直接從 TravelGroups/TravelGroupImages 刪掉即可，不影響其他表。
// 圖片網址沿用 VlogPostStore 裡已經用 curl 驗證過 200 OK 的 Unsplash 圖庫網址，避免又出現破圖。
public static class TravelGroupDbSeeder
{
    private sealed record SeedGroup(
        string Title, string Description, string Country, string Region,
        int DaysFromNow, int Days, int MinPeople, int MaxPeople, int CurrentPeople, string ImageUrl);

    private static readonly SeedGroup[] Seeds =
    {
        new("日本關西賞櫻小team", "想找一起追櫻花的旅伴，行程步調輕鬆，第一次揪團也歡迎～",
            "日本", "關西", 30, 6, 2, 6, 4,
            "https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?w=600&q=80"),
        new("義大利南部海岸線", "阿瑪菲海岸自駕之旅，找會開車或想放鬆坐車看海的夥伴",
            "義大利", "阿瑪菲海岸", 60, 9, 3, 8, 5,
            "https://images.unsplash.com/photo-1499678329028-101435549a4e?w=600&q=80"),
        new("冰島極光探險", "包車環島追極光，需要能接受早起、晚睡的行程夥伴",
            "冰島", "雷克雅維克", 90, 7, 4, 10, 7,
            "https://images.unsplash.com/photo-1470770841072-f978cf4d019e?w=600&q=80"),
        new("台灣環島騎車", "沿海岸線慢慢騎，不趕路，晚上找當地小吃",
            "台灣", "環島", 14, 8, 2, 12, 9,
            "https://images.unsplash.com/photo-1540187334920-54e87c2771c0?w=600&q=80"),
        new("柬埔寨吳哥窟文化行", "看日出配古蹟，找對歷史文化有興趣的旅伴",
            "柬埔寨", "暹粒", 45, 5, 2, 6, 3,
            "https://images.unsplash.com/photo-1528181304800-259b08848526?w=600&q=80"),
        new("紐西蘭南島自駕", "租車環南島，需要會手排/國際駕照的夥伴分擔開車",
            "紐西蘭", "南島", 75, 10, 2, 5, 2,
            "https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?w=600&q=80"),
    };

    // 詳情頁的相簿/行程/預算/團員都要有真實資料可以顯示，用同一批已驗證過的 Unsplash 圖庫網址。
    // 至少要 5 張(1 張封面 + 4 張)相簿格才會裝滿，第 4 格才會疊出「更多圖片」——
    // 之前只放 3 張額外照片(總共 4 張)，永遠湊不滿、也就永遠不會出現「更多圖片」。
    private static readonly string[] GalleryExtras =
    {
        "https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?w=500&q=80",
        "https://images.unsplash.com/photo-1528181304800-259b08848526?w=500&q=80",
        "https://images.unsplash.com/photo-1540187334920-54e87c2771c0?w=500&q=80",
        "https://images.unsplash.com/photo-1470770841072-f978cf4d019e?w=500&q=80",
        "https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?w=500&q=80",
        "https://images.unsplash.com/photo-1499678329028-101435549a4e?w=500&q=80",
    };

    public static async Task SeedAsync(LazyTravelDBContext context)
    {
        if (await context.TravelGroups.AnyAsync())
        {
            await EnsureQuickTestMembersAsync(context);
            return;
        }

        var memberIds = await context.Users.Select(m => m.Id).OrderBy(id => id).Take(4).ToListAsync();
        if (memberIds.Count == 0)
        {
            // Members 表也是空的（例如全新資料庫、還沒跑過 VlogPostDbSeeder）就不硬塞，避免 FK 對不上真實會員。
            return;
        }
        var ownerMemberId = memberIds[0];

        foreach (var seed in Seeds)
        {
            var group = new TravelGroup
            {
                OwnerMemberId = ownerMemberId,
                GroupTitle = seed.Title,
                Description = seed.Description,
                StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(seed.DaysFromNow)),
                EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(seed.DaysFromNow + seed.Days)),
                MinPeople = seed.MinPeople,
                MaxPeople = seed.MaxPeople,
                CurrentPeople = seed.CurrentPeople,
                JoinRule = 1,
                GroupStatus = 0,
                IsPublic = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsDelete = false,
                ReviewStatus = TravelGroupReviewStatus.Normal,
                Country = seed.Country,
                Region = seed.Region,
            };
            context.TravelGroups.Add(group);
            await context.SaveChangesAsync(); // 存檔後 group.GroupId 才會是資料庫真正配的 IDENTITY 值

            context.TravelGroupImages.Add(new TravelGroupImage
            {
                GroupId = group.GroupId,
                ImageUrl = seed.ImageUrl,
                ImageType = 0,
                AltText = seed.Title,
                SortOrder = 0,
                IsCover = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
            });
            for (var i = 0; i < GalleryExtras.Length; i++)
            {
                context.TravelGroupImages.Add(new TravelGroupImage
                {
                    GroupId = group.GroupId,
                    ImageUrl = GalleryExtras[i],
                    ImageType = 0,
                    AltText = seed.Title,
                    SortOrder = i + 1,
                    IsCover = false,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                });
            }

            // 行程表：前兩天的示範站點
            context.TravelGroupItineraryItems.AddRange(
                new TravelGroupItineraryItem
                {
                    GroupId = group.GroupId, DayNumber = 1, SortOrder = 0,
                    StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0),
                    Title = "抵達與市區散步", LocationName = seed.Region,
                    Description = $"抵達{seed.Country}{seed.Region}，放行李後在附近散步熟悉環境。",
                    CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
                },
                new TravelGroupItineraryItem
                {
                    GroupId = group.GroupId, DayNumber = 1, SortOrder = 1,
                    StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(20, 0),
                    Title = "在地晚餐", LocationName = seed.Region,
                    Description = "品嚐當地特色料理，大家互相認識。",
                    CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
                },
                new TravelGroupItineraryItem
                {
                    GroupId = group.GroupId, DayNumber = 2, SortOrder = 0,
                    StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(12, 0),
                    Title = "主要景點半日遊", LocationName = seed.Region,
                    Description = "當地最推薦的景點，自由拍照與活動。",
                    CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
                },
                new TravelGroupItineraryItem
                {
                    GroupId = group.GroupId, DayNumber = 2, SortOrder = 1,
                    StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(17, 0),
                    Title = "自由活動／購物", LocationName = seed.Region,
                    Description = "自由時間，逛街或休息都可以。",
                    CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
                });

            // 預算試算：住宿(0) / 交通(1) / 餐飲(2) / 門票與活動(3)
            context.TravelGroupBudgets.AddRange(
                new TravelGroupBudget { GroupId = group.GroupId, BudgetCategory = 0, BudgetName = "住宿", Amount = 8000, CurrencyCode = "TWD", IsRequired = true, SortOrder = 0, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new TravelGroupBudget { GroupId = group.GroupId, BudgetCategory = 1, BudgetName = "交通", Amount = 5500, CurrencyCode = "TWD", IsRequired = true, SortOrder = 1, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new TravelGroupBudget { GroupId = group.GroupId, BudgetCategory = 2, BudgetName = "餐飲", Amount = 3800, CurrencyCode = "TWD", IsRequired = true, SortOrder = 2, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new TravelGroupBudget { GroupId = group.GroupId, BudgetCategory = 3, BudgetName = "門票與活動", Amount = 1200, CurrencyCode = "TWD", IsRequired = false, SortOrder = 3, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now });

            // 團員：團主本人一定是團員，其餘示範會員依序加入湊到 CurrentPeople（最多湊到示範會員數）
            context.GroupMembers.Add(new GroupMember
            {
                GroupId = group.GroupId, MemberId = ownerMemberId, MemberRole = 1,
                JoinedAt = DateTime.Now, IsRemoved = false, CreatedAt = DateTime.Now,
            });
            var extraCount = Math.Min(seed.CurrentPeople - 1, memberIds.Count - 1);
            for (var i = 1; i <= extraCount; i++)
            {
                context.GroupMembers.Add(new GroupMember
                {
                    GroupId = group.GroupId, MemberId = memberIds[i], MemberRole = 0,
                    JoinedAt = DateTime.Now, IsRemoved = false, CreatedAt = DateTime.Now,
                });
            }
        }

        await context.SaveChangesAsync();
        await EnsureQuickTestMembersAsync(context);
    }

    private static async Task EnsureQuickTestMembersAsync(LazyTravelDBContext context)
    {
        var now = DateTime.Now;
        var outsider = await EnsureQuickTestMemberAsync(context,
            email: "lt-test-outsider@lazytravel.local",
            name: "測試旅人A・未加入",
            avatarUrl: "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=200&q=80",
            bio: "快速測試用：未加入該揪團的普通會員，可測申請加入、收藏與一般檢視狀態。",
            occupation: "自由旅人",
            mbti: "INFP",
            gender: 2,
            birthDate: new DateTime(1998, 5, 12),
            now: now);

        var joined = await EnsureQuickTestMemberAsync(context,
            email: "lt-test-member@lazytravel.local",
            name: "測試旅人B・已加入",
            avatarUrl: "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=200&q=80",
            bio: "快速測試用：已加入測試揪團的團員，可測退出揪團與團員檢視狀態。",
            occupation: "前端工程師",
            mbti: "ENFP",
            gender: 1,
            birthDate: new DateTime(1995, 9, 22),
            now: now);

        var owner = await EnsureQuickTestMemberAsync(context,
            email: "lt-test-owner@lazytravel.local",
            name: "測試旅人C・團長",
            avatarUrl: "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=200&q=80",
            bio: "快速測試用：測試揪團的建立者，可測團長檢視與管理狀態。",
            occupation: "行程規劃師",
            mbti: "ENTJ",
            gender: 2,
            birthDate: new DateTime(1993, 2, 8),
            now: now);

        const string testGroupTitle = "快速測試：關西賞櫻身份測試團";
        var group = await context.TravelGroups.FirstOrDefaultAsync(g => g.GroupTitle == testGroupTitle && !g.IsDelete);
        if (group is null)
        {
            group = new TravelGroup
            {
                OwnerMemberId = owner.Id,
                GroupTitle = testGroupTitle,
                Description = "提供前台快速測試用：A 未加入、B 已加入、C 是團長。可用來檢查揪團詳細頁不同身份的按鈕與版面。",
                StartDate = DateOnly.FromDateTime(now.AddDays(21)),
                EndDate = DateOnly.FromDateTime(now.AddDays(28)),
                MinPeople = 2,
                MaxPeople = 6,
                CurrentPeople = 2,
                JoinRule = 1,
                GroupStatus = 1,
                IsPublic = true,
                CreatedAt = now,
                UpdatedAt = now,
                IsDelete = false,
                ReviewStatus = TravelGroupReviewStatus.Normal,
                Country = "日本",
                Region = "關西",
            };
            context.TravelGroups.Add(group);
            await context.SaveChangesAsync();
        }
        else if (group.OwnerMemberId != owner.Id || group.CurrentPeople < 2)
        {
            group.OwnerMemberId = owner.Id;
            group.CurrentPeople = Math.Max(group.CurrentPeople, 2);
            group.UpdatedAt = now;
            await context.SaveChangesAsync();
        }

        await EnsureQuickTestGroupMemberAsync(context, group.GroupId, owner.Id, memberRole: 1, now);
        await EnsureQuickTestGroupMemberAsync(context, group.GroupId, joined.Id, memberRole: 0, now);

        var outsiderMembership = await context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == group.GroupId && m.MemberId == outsider.Id && !m.IsRemoved);
        if (outsiderMembership is not null)
        {
            outsiderMembership.IsRemoved = true;
            outsiderMembership.LeftAt = now;
            outsiderMembership.RemovedAt = now;
            outsiderMembership.RemovedByMemberId = owner.Id;
            outsiderMembership.RemoveReason = "快速測試資料：保留此會員為未加入狀態";
            await context.SaveChangesAsync();
        }

        if (!await context.TravelGroupImages.AnyAsync(i => i.GroupId == group.GroupId && i.IsCover && !i.IsDeleted))
        {
            context.TravelGroupImages.Add(new TravelGroupImage
            {
                GroupId = group.GroupId,
                ImageUrl = "https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?w=1200&q=80",
                ImageType = 0,
                AltText = testGroupTitle,
                SortOrder = 0,
                IsCover = true,
                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = now,
            });
            await context.SaveChangesAsync();
        }

        if (!await context.TravelGroupItineraryItems.AnyAsync(i => i.GroupId == group.GroupId))
        {
            context.TravelGroupItineraryItems.AddRange(
                new TravelGroupItineraryItem
                {
                    GroupId = group.GroupId,
                    DayNumber = 1,
                    SortOrder = 0,
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(12, 0),
                    Title = "抵達大阪・心齋橋散步",
                    LocationName = "大阪",
                    Description = "團員集合後前往市區，下午用輕鬆散步確認行程節奏。",
                    CreatedAt = now,
                    UpdatedAt = now,
                },
                new TravelGroupItineraryItem
                {
                    GroupId = group.GroupId,
                    DayNumber = 2,
                    SortOrder = 0,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(17, 0),
                    Title = "京都清水寺・祇園夜櫻",
                    LocationName = "京都",
                    Description = "測試用行程節點，方便詳細頁檢查資料對應。",
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            await context.SaveChangesAsync();
        }
    }

    private static async Task<Member> EnsureQuickTestMemberAsync(LazyTravelDBContext context, string email, string name,
        string avatarUrl, string bio, string occupation, string mbti, byte gender, DateTime birthDate, DateTime now)
    {
        var member = await context.Users.FirstOrDefaultAsync(m => m.Email == email);
        if (member is null)
        {
            member = new Member
            {
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = true,
                PhoneNumber = null,
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnd = null,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                Name = name,
                LineId = null,
                InstagramUrl = null,
                FacebookUrl = null,
                ContactBookVisibility = 0,
                IsPrivateAccount = false,
                AvatarUrl = avatarUrl,
                BirthDate = birthDate,
                Gender = gender,
                Occupation = occupation,
                Mbti = mbti,
                Bio = bio,
                Status = 1,
                CreatedAt = now,
                LastLoginAt = null,
                LastLoginIp = null,
                IsDelete = false,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            };
            member.PasswordHash = new PasswordHasher<Member>().HashPassword(member, "Test1234!");
            context.Users.Add(member);
            await context.SaveChangesAsync();
            return member;
        }

        member.UserName = email;
        member.NormalizedUserName = email.ToUpperInvariant();
        member.NormalizedEmail = email.ToUpperInvariant();
        member.EmailConfirmed = true;
        member.Name = name;
        member.ContactBookVisibility = 0;
        member.IsPrivateAccount = false;
        member.AvatarUrl = avatarUrl;
        member.BirthDate = birthDate;
        member.Gender = gender;
        member.Occupation = occupation;
        member.Mbti = mbti;
        member.Bio = bio;
        member.Status = 1;
        member.IsDelete = false;
        if (string.IsNullOrWhiteSpace(member.SecurityStamp)) member.SecurityStamp = Guid.NewGuid().ToString("N");
        if (string.IsNullOrWhiteSpace(member.ConcurrencyStamp)) member.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        if (string.IsNullOrWhiteSpace(member.PasswordHash)) member.PasswordHash = new PasswordHasher<Member>().HashPassword(member, "Test1234!");
        await context.SaveChangesAsync();
        return member;
    }

    private static async Task EnsureQuickTestGroupMemberAsync(LazyTravelDBContext context, int groupId, int memberId, byte memberRole, DateTime now)
    {
        var membership = await context.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == groupId && m.MemberId == memberId);
        if (membership is null)
        {
            context.GroupMembers.Add(new GroupMember
            {
                GroupId = groupId,
                MemberId = memberId,
                MemberRole = memberRole,
                JoinedAt = now,
                LeftAt = null,
                IsRemoved = false,
                RemovedByMemberId = null,
                RemovedAt = null,
                RemoveReason = null!,
                CreatedAt = now,
            });
            await context.SaveChangesAsync();
            return;
        }

        membership.MemberRole = memberRole;
        membership.IsRemoved = false;
        membership.LeftAt = null;
        membership.RemovedByMemberId = null;
        membership.RemovedAt = null;
        membership.RemoveReason = null!;
        await context.SaveChangesAsync();
    }
}



