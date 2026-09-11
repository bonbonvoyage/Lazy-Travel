using LazyTravel.Shared.Models.EfModels;
using LazyTravel.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Controllers;

public class FavoritesController : Controller
{
    private const string VisitorMemberCookieName = "ltvmid";
    private const string LegacyFavoriteGroupsCookieName = "lt_favorite_groups";
    private readonly LazyTravelDBContext _context;

    public FavoritesController(LazyTravelDBContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string type = "trips", string sort = "newest", string? keyword = null, string? country = null)
    {
        type = type == "articles" ? "articles" : "trips";
        sort = sort == "oldest" ? "oldest" : "newest";
        keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
        country = string.IsNullOrWhiteSpace(country) ? null : country.Trim();

        var trips = await LoadFavoriteTripsAsync(sort, keyword, country);
        var articles = await LoadFavoriteArticlesAsync(sort, keyword, country);
        var countries = trips.Select(t => t.Country)
            .Concat(articles.Select(a => a.Country))
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        var model = new FavoriteTripsViewModel
        {
            Type = type,
            Sort = sort,
            Keyword = keyword,
            Country = country,
            Trips = type == "trips" ? trips : new List<FavoriteTripCardViewModel>(),
            Articles = type == "articles" ? articles : new List<FavoriteArticleCardViewModel>(),
            AllCountries = countries,
        };

        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
        {
            return PartialView("_FavoritesContent", model);
        }

        return View(model);
    }

    private async Task<List<FavoriteTripCardViewModel>> LoadFavoriteTripsAsync(string sort, string? keyword, string? country)
    {
        var visitorId = await GetOrCreateVisitorMemberIdAsync();
        var query = _context.TravelGroupInteractions.AsNoTracking()
            .Where(i => i.MemberId == visitorId && i.ActionType == TravelGroupInteractionType.Favorite)
            .Include(i => i.Group)
                .ThenInclude(g => g.OwnerMember)
            .Include(i => i.Group)
                .ThenInclude(g => g.TravelGroupImages)
            .Where(i => i.Group.IsPublic && !i.Group.IsDelete && i.Group.ReviewStatus == TravelGroupReviewStatus.Normal);

        query = sort == "oldest" ? query.OrderBy(i => i.CreatedAt) : query.OrderByDescending(i => i.CreatedAt);
        var groups = await query.Select(i => i.Group).ToListAsync();
        var ordered = groups.Select(ToTripCard);

        ordered = ApplyTripFilters(ordered, keyword, country);
        return ordered.ToList();
    }

    private async Task<List<FavoriteArticleCardViewModel>> LoadFavoriteArticlesAsync(string sort, string? keyword, string? country)
    {
        var visitorId = await GetOrCreateVisitorMemberIdAsync();
        var query = _context.PostInteractions.AsNoTracking()
            .Where(i => i.MemberId == visitorId && i.ActionType == PostInteractionType.Favorite)
            .Include(i => i.Post)
                .ThenInclude(p => p.Member)
            .Include(i => i.Post)
                .ThenInclude(p => p.VlogPostImages)
            .Where(i => !i.Post.IsDelete && i.Post.Status == VlogPostStatus.Published);

        query = sort == "oldest" ? query.OrderBy(i => i.CreatedAt) : query.OrderByDescending(i => i.CreatedAt);
        var articles = await query.Select(i => i.Post).ToListAsync();
        var cards = articles.Select(ToArticleCard);
        cards = ApplyArticleFilters(cards, keyword, country);
        return cards.ToList();
    }

    private async Task<int> GetOrCreateVisitorMemberIdAsync()
    {
        if (Request.Cookies.TryGetValue(VisitorMemberCookieName, out var raw) && int.TryParse(raw, out var existingId))
        {
            var stillValid = await _context.Users.AnyAsync(m => m.Id == existingId);
            if (stillValid)
            {
                await MigrateLegacyFavoriteGroupsCookieAsync(existingId);
                return existingId;
            }
        }

        var candidateId = await _context.Users.AsNoTracking()
            .Where(m => m.Email != LazyTravel.Shared.Models.MemberLookup.OfficialAccountEmail)
            .OrderBy(m => m.Id)
            .Select(m => m.Id)
            .FirstOrDefaultAsync();

        if (candidateId == 0)
        {
            candidateId = await _context.Users.AsNoTracking()
                .OrderBy(m => m.Id)
                .Select(m => m.Id)
                .FirstOrDefaultAsync();
        }

        if (candidateId == 0)
        {
            var member = new LazyTravel.Shared.Models.EfModels.Member
            {
                Email = "demo-traveler@lazytravel.local",
                Name = "小旅人",
                CreatedAt = DateTime.Now,
            };
            _context.Users.Add(member);
            await _context.SaveChangesAsync();
            candidateId = member.Id;
        }

        Response.Cookies.Append(VisitorMemberCookieName, candidateId.ToString(), new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(2),
            IsEssential = true,
            HttpOnly = true,
        });
        await MigrateLegacyFavoriteGroupsCookieAsync(candidateId);
        return candidateId;
    }

    private async Task MigrateLegacyFavoriteGroupsCookieAsync(int memberId)
    {
        if (!Request.Cookies.TryGetValue(LegacyFavoriteGroupsCookieName, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        var ids = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        if (ids.Count == 0)
        {
            Response.Cookies.Delete(LegacyFavoriteGroupsCookieName);
            return;
        }

        ids = await _context.TravelGroups.AsNoTracking()
            .Where(g => ids.Contains(g.GroupId) && g.IsPublic && !g.IsDelete && g.ReviewStatus == TravelGroupReviewStatus.Normal)
            .Select(g => g.GroupId)
            .ToListAsync();
        if (ids.Count == 0)
        {
            Response.Cookies.Delete(LegacyFavoriteGroupsCookieName);
            return;
        }

        var existingIds = await _context.TravelGroupInteractions
            .Where(i => i.MemberId == memberId && i.ActionType == TravelGroupInteractionType.Favorite && ids.Contains(i.GroupId))
            .Select(i => i.GroupId)
            .ToListAsync();
        var existingSet = existingIds.ToHashSet();
        var now = DateTime.Now;
        foreach (var id in ids.Where(id => !existingSet.Contains(id)))
        {
            _context.TravelGroupInteractions.Add(new TravelGroupInteraction
            {
                GroupId = id,
                MemberId = memberId,
                ActionType = TravelGroupInteractionType.Favorite,
                CreatedAt = now,
            });
        }

        await _context.SaveChangesAsync();
        Response.Cookies.Delete(LegacyFavoriteGroupsCookieName);
    }

    private static IEnumerable<FavoriteTripCardViewModel> ApplyTripFilters(IEnumerable<FavoriteTripCardViewModel> cards, string? keyword, string? country)
    {
        if (!string.IsNullOrWhiteSpace(country)) cards = cards.Where(c => c.Country == country);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            cards = cards.Where(c => Contains(c.Title, keyword) || Contains(c.Country, keyword) || Contains(c.Region, keyword));
        }
        return cards;
    }

    private static IEnumerable<FavoriteArticleCardViewModel> ApplyArticleFilters(IEnumerable<FavoriteArticleCardViewModel> cards, string? keyword, string? country)
    {
        if (!string.IsNullOrWhiteSpace(country)) cards = cards.Where(c => c.Country == country);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            cards = cards.Where(c => Contains(c.Title, keyword) || Contains(c.Country, keyword) || Contains(c.Region, keyword));
        }
        return cards;
    }

    private static bool Contains(string source, string keyword) => source.Contains(keyword, StringComparison.OrdinalIgnoreCase);

    private static FavoriteTripCardViewModel ToTripCard(TravelGroup group)
    {
        var cover = group.TravelGroupImages
            .Where(i => !i.IsDeleted)
            .OrderByDescending(i => i.IsCover)
            .ThenBy(i => i.SortOrder)
            .FirstOrDefault();

        return new FavoriteTripCardViewModel
        {
            GroupId = group.GroupId,
            Title = group.GroupTitle,
            Country = group.Country ?? "",
            Region = group.Region ?? "",
            DateText = FormatDateRange(group.StartDate, group.EndDate),
            CoverImageUrl = cover?.ImageUrl ?? "",
            CurrentPeople = group.CurrentPeople,
            MaxPeople = group.MaxPeople,
            OwnerName = string.IsNullOrWhiteSpace(group.OwnerMember?.Name) ? "旅人" : group.OwnerMember.Name,
            OwnerAvatarUrl = group.OwnerMember?.AvatarUrl,
            Status = group.GroupStatus,
            StatusText = group.GroupStatus switch
            {
                1 => "等待中",
                2 => "成行中",
                3 => "已完成",
                4 => "已取消",
                _ => "等待中"
            },
        };
    }

    private static FavoriteArticleCardViewModel ToArticleCard(VlogPost post)
    {
        var cover = post.VlogPostImages
            .Where(i => !i.IsDeleted)
            .OrderByDescending(i => i.IsCover)
            .ThenBy(i => i.SortOrder)
            .FirstOrDefault(i => IsImageUrl(i.ImageUrl));

        return new FavoriteArticleCardViewModel
        {
            PostId = post.PostId,
            Title = post.Title,
            Country = post.Destination ?? "",
            Region = "行程分享",
            DateText = FormatArticleDate(post.TravelDate, post.TravelDays),
            PeopleText = post.TravelPeople.ToLabel(),
            CoverImageUrl = cover?.ImageUrl ?? (IsImageUrl(post.MediaUrl) ? post.MediaUrl : ""),
            AuthorName = string.IsNullOrWhiteSpace(post.Member?.Name) ? "旅人" : post.Member.Name,
            AuthorAvatarUrl = post.Member?.AvatarUrl,
        };
    }

    private static string FormatDateRange(DateOnly? start, DateOnly? end)
    {
        if (!start.HasValue && !end.HasValue) return "日期未定";
        if (start.HasValue && !end.HasValue) return start.Value.ToString("yyyy/MM/dd");
        if (!start.HasValue && end.HasValue) return end.Value.ToString("yyyy/MM/dd");
        return $"{start:yyyy/MM/dd} - {end:yyyy/MM/dd}";
    }

    private static string FormatArticleDate(DateTime? start, int days)
    {
        if (!start.HasValue) return days > 0 ? $"{days} 天" : "日期未定";
        var totalDays = Math.Max(1, days);
        var end = start.Value.AddDays(totalDays - 1);
        return totalDays == 1 ? start.Value.ToString("yyyy/MM/dd") : $"{start:yyyy/MM/dd} - {end:yyyy/MM/dd}";
    }

    private static bool IsImageUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url) && (url.StartsWith("/") && !url.StartsWith("//") ||
            Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == "https" || uri.Scheme == "http"));
}



