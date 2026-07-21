using LazyTravel.Models;
using LazyTravel.Services;
using Microsoft.AspNetCore.Mvc;

namespace LazyTravel.Controllers
{
    // 前台「找行程」頁面：只給看已發布、未刪除的文章，跟後台管理邏輯分開。
    public class ExploreController : Controller
    {
        // GET /Explore
        public IActionResult Index(string? region, string? keyword)
        {
            var published = VlogPostStore.GetAll()
                .Where(p => !p.IsDelete && p.Status == VlogPostStatus.Published)
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
                .OrderByDescending(p => PostInteractionStore.GetLikeCount(p.PostID) + PostInteractionStore.GetFavoriteCount(p.PostID))
                .ThenByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Take(10)
                .ToList();

            // 地區下拉選單：從所有已發布文章的目的地整理出來，不是寫死的清單
            var regions = VlogPostStore.GetAll()
                .Where(p => !p.IsDelete && p.Status == VlogPostStatus.Published)
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
        public IActionResult Details(int id)
        {
            var post = VlogPostStore.GetById(id);
            if (post is null || post.IsDelete || post.Status != VlogPostStatus.Published)
            {
                return NotFound();
            }

            ViewData["Title"] = post.Title;
            ViewBag.Nodes = ItineraryNodeStore.GetByPostId(id)
                .OrderBy(n => n.DayNumber).ThenBy(n => n.ArrivalTime).ToList();
            ViewBag.LikeCount = PostInteractionStore.GetLikeCount(id);
            ViewBag.FavoriteCount = PostInteractionStore.GetFavoriteCount(id);
            return View(post);
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
