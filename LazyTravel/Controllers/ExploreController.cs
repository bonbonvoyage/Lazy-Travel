using LazyTravel.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Controllers
{
    // 前台「找行程」頁面：只給看已發布、未刪除的文章，跟後台管理邏輯分開。
    public class ExploreController : Controller
    {
        private readonly LazyTravelContext _context;

        public ExploreController(LazyTravelContext context)
        {
            _context = context;
        }

        // GET /Explore
        public async Task<IActionResult> Index(string? region, string? keyword)
        {
            var published = (await _context.VlogPosts.AsNoTracking()
                .Where(p => !p.IsDelete && p.Status == VlogPostStatus.Published)
                .ToListAsync())
                .AsEnumerable();

            if (!string.IsNullOrWhiteSpace(region))
            {
                published = published.Where(p => p.Destination == region);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                published = published.Where(p =>
                    p.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    p.Destination.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            var list = published.ToList();
            var postIds = list.Select(p => p.PostID).ToList();

            var interactionCounts = await _context.PostInteractions
                .Where(i => postIds.Contains(i.PostID))
                .GroupBy(i => new { i.PostID, i.ActionType })
                .Select(g => new { g.Key.PostID, g.Key.ActionType, Count = g.Count() })
                .ToListAsync();
            int InteractionCount(int postId) =>
                interactionCounts.Where(c => c.PostID == postId).Sum(c => c.Count);

            // 精選文章：優先挑官方帳號發的，沒有的話退而求其次選最新更新的一篇
            var featured = list
                .Where(p => MemberLookup.IsOfficial(p.MemberID))
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .FirstOrDefault()
                ?? list.OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt).FirstOrDefault();

            var sideList = list
                .Where(p => featured is null || p.PostID != featured.PostID)
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Take(3)
                .ToList();

            var ranking = list
                .OrderByDescending(p => InteractionCount(p.PostID))
                .ThenByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Take(10)
                .ToList();

            // 地區下拉選單：從所有已發布文章的目的地整理出來，不是寫死的清單
            var regions = list
                .Select(p => p.Destination)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            var vm = new ExploreIndexViewModel
            {
                Region = region,
                Keyword = keyword,
                Regions = regions,
                Featured = featured,
                FeaturedExcerpt = BuildExcerpt(featured?.Content),
                SideList = sideList,
                Ranking = ranking,
            };

            ViewData["Title"] = "找行程";
            return View(vm);
        }

        // GET /Explore/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var post = await _context.VlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.PostID == id);
            if (post is null || post.IsDelete || post.Status != VlogPostStatus.Published)
            {
                return NotFound();
            }

            var visitorId = GetOrCreateVisitorId();

            ViewData["Title"] = post.Title;
            ViewBag.Nodes = await _context.ItineraryNodes.AsNoTracking()
                .Where(n => n.PostID == id)
                .OrderBy(n => n.DayNumber).ThenBy(n => n.ArrivalTime).ToListAsync();
            ViewBag.LikeCount = await _context.PostInteractions.CountAsync(i => i.PostID == id && i.ActionType == PostInteractionType.Like);
            ViewBag.FavoriteCount = await _context.PostInteractions.CountAsync(i => i.PostID == id && i.ActionType == PostInteractionType.Favorite);
            ViewBag.IsLiked = await _context.PostInteractions.AnyAsync(i => i.PostID == id && i.MemberID == visitorId && i.ActionType == PostInteractionType.Like);
            ViewBag.IsFavorited = await _context.PostInteractions.AnyAsync(i => i.PostID == id && i.MemberID == visitorId && i.ActionType == PostInteractionType.Favorite);
            return View(post);
        }

        // POST /Explore/ToggleLike/5 —— 讀者點一次讚就加一筆互動紀錄，再點一次取消（跟收藏共用同一套邏輯）。
        // 目前沒有真的會員登入系統，先用瀏覽器 Cookie 記一個匿名訪客 ID，同一支瀏覽器重複點才會正確切換讚/取消讚。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(int id)
        {
            await ToggleInteractionAsync(id, PostInteractionType.Like);
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST /Explore/ToggleFavorite/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int id)
        {
            await ToggleInteractionAsync(id, PostInteractionType.Favorite);
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task ToggleInteractionAsync(int postId, PostInteractionType actionType)
        {
            var postExists = await _context.VlogPosts.AnyAsync(p => p.PostID == postId && !p.IsDelete && p.Status == VlogPostStatus.Published);
            if (!postExists)
            {
                return;
            }

            var visitorId = GetOrCreateVisitorId();
            var existing = await _context.PostInteractions
                .FirstOrDefaultAsync(i => i.PostID == postId && i.MemberID == visitorId && i.ActionType == actionType);

            if (existing is not null)
            {
                _context.PostInteractions.Remove(existing);
            }
            else
            {
                _context.PostInteractions.Add(new PostInteraction { PostID = postId, MemberID = visitorId, ActionType = actionType });
            }

            await _context.SaveChangesAsync();
        }

        private const string VisitorCookieName = "ltvid";

        // 匿名訪客 ID：跟示範資料用的 MemberID（1~4，或種子資料裡到數十的範圍）錯開，
        // 避免不小心跟既有的假互動紀錄撞到同一個 (PostID, MemberID, ActionType) 組合。
        private int GetOrCreateVisitorId()
        {
            if (Request.Cookies.TryGetValue(VisitorCookieName, out var raw) && int.TryParse(raw, out var existingId) && existingId > 0)
            {
                return existingId;
            }

            var newId = Random.Shared.Next(100_000, 999_999);
            Response.Cookies.Append(VisitorCookieName, newId.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(2),
                IsEssential = true,
                HttpOnly = true,
            });
            return newId;
        }

        // 把 Quill 存的 HTML 內文去掉標籤，剪一小段當摘要用
        private static string BuildExcerpt(string? html, int maxLength = 80)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            var text = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
            text = System.Net.WebUtility.HtmlDecode(text).Trim();
            return text.Length > maxLength ? text[..maxLength] + "…" : text;
        }
    }
}
