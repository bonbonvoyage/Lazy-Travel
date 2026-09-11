using LazyTravel.Shared.Models;
using LazyTravel.Shared.Models.EfModels;
using LazyTravel.Shared.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Controllers;

public class HomeController : Controller
{
    private readonly LazyTravelDBContext _context;

    public HomeController(LazyTravelDBContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "首頁";
        var countries = await _context.TravelGroups.AsNoTracking()
            .Where(g => g.IsPublic && !g.IsDelete && g.ReviewStatus == TravelGroupReviewStatus.Normal && g.Country != null && g.Country != "")
            .Select(g => g.Country!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return View(new LazyTravel.ViewModels.TravelGroupIndexViewModel
        {
            AllCountries = countries,
            Countries = countries,
        });
    }

    public async Task<IActionResult> Data()
    {
        var trips = await _context.TravelGroups.AsNoTracking()
            .Include(g => g.TravelGroupImages)
            .Where(g => g.IsPublic && !g.IsDelete && g.ReviewStatus == TravelGroupReviewStatus.Normal)
            .OrderByDescending(g => g.CurrentPeople)
            .Take(8)
            .ToListAsync();

        var popularTrips = trips.Select(g =>
        {
            var cover = g.TravelGroupImages
                .Where(i => !i.IsDeleted)
                .OrderByDescending(i => i.IsCover)
                .ThenBy(i => i.SortOrder)
                .FirstOrDefault();

            var days = g.StartDate.HasValue && g.EndDate.HasValue
                ? g.EndDate.Value.DayNumber - g.StartDate.Value.DayNumber + 1
                : (int?)null;

            return new HomeTripCardVm
            {
                GroupId = g.GroupId,
                Country = g.Country,
                GroupTitle = g.GroupTitle,
                CoverImageUrl = cover?.ImageUrl ?? "",
                DaysText = days.HasValue ? $"{days}天{Math.Max(0, days.Value - 1)}夜" : "",
                CurrentPeople = g.CurrentPeople,
                MaxPeople = g.MaxPeople,
            };
        }).ToList();

        var posts = await _context.VlogPosts.AsNoTracking()
            .Include(p => p.Member)
            .Where(p => !p.IsDelete && p.Status == VlogPostStatus.Published)
            .ToListAsync();

        var postIds = posts.Select(p => p.PostId).ToList();
        var likeCounts = await _context.PostInteractions
            .Where(i => postIds.Contains(i.PostId) && i.ActionType == PostInteractionType.Like)
            .GroupBy(i => i.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToListAsync();
        int LikeCount(int postId) => likeCounts.FirstOrDefault(c => c.PostId == postId)?.Count ?? 0;

        var popularArticles = posts
            .OrderByDescending(p => LikeCount(p.PostId))
            .ThenByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .Take(8)
            .Select(p => new HomeArticleCardVm
            {
                PostId = p.PostId,
                Title = p.Title,
                MediaUrl = p.MediaUrl,
                AuthorName = p.Member.Name,
                DateText = (p.UpdatedAt ?? p.CreatedAt).ToString("yyyy/MM/dd"),
                LikeCount = LikeCount(p.PostId),
            })
            .ToList();

        var vm = new HomeIndexViewModel
        {
            PopularTrips = popularTrips,
            PopularArticles = popularArticles,
        };

        return Ok(vm);
    }

    public IActionResult Error() => View();
}
