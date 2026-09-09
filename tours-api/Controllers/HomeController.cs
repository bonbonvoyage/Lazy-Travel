using LazyTravel.Shared.Models;
using LazyTravel.Shared.Models.EfModels;
using LazyTravel.Shared.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Controllers
{
    public class HomeController : Controller
    {
        private readonly LazyTravelDBContext _context;

        public HomeController(LazyTravelDBContext context)
        {
            _context = context;
        }

        // GET /Home/Index —— 只負責回傳頁面外殼(側欄、Hero、卡片區的空容器)，
        // 版面上看到的行程/文章資料一律由 wwwroot/js/home.js 呼叫下面的 Data() 拿真實資料。
        public IActionResult Index()
        {
            ViewData["Title"] = "首頁";
            return View();
        }

        // GET /Home/Data —— 首頁卡片區的真實資料，前端 fetch 這支拿 JSON。
        // 依 FRONTEND_BACKEND_SPLIT.md 慣例：直接 Ok(vm)，不包 {success,data} 外層。
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
                    DaysText = days.HasValue ? $"{days}天{days - 1}夜" : "",
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

        public IActionResult Error()
        {
            return View();
        }
    }
}
