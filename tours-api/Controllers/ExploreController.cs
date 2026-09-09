using LazyTravel.Shared.Models;
using LazyTravel.Shared.Models.EfModels;
using LazyTravel.Shared.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LazyTravel.Controllers
{
    // 前台「找行程」頁面：只給看已發布、未刪除的文章，跟後台管理邏輯分開。
    public class ExploreController : Controller
    {
        private readonly LazyTravelDBContext _context;

        public ExploreController(LazyTravelDBContext context)
        {
            _context = context;
        }

        // GET /Explore
        public async Task<IActionResult> Index(string? country, string? startDate, string? endDate, string scope = "all", int page = 1)
        {
            const int pageSize = 6;
            scope = scope == "recommended" ? "recommended" : "all";
            var published = await _context.VlogPosts.AsNoTracking()
                .Include(p => p.Member)
                .Where(p => !p.IsDelete && p.Status == VlogPostStatus.Published)
                .ToListAsync();
            var countries = published.Select(p => p.Destination)
                .Where(d => !string.IsNullOrWhiteSpace(d)).Distinct().OrderBy(d => d).ToList();
            country = country?.Trim();
            IEnumerable<VlogPost> filtered = published;
            if (!string.IsNullOrEmpty(country))
                filtered = filtered.Where(p => p.Destination == country);

            var hasStart = DateTime.TryParseExact(startDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var start);
            var hasEnd = DateTime.TryParseExact(endDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var end);
            if (hasStart && !hasEnd) { end = start; hasEnd = true; }
            if (hasEnd && !hasStart) { start = end; hasStart = true; }
            if (hasStart && hasEnd)
            {
                if (start > end) (start, end) = (end, start);
                filtered = filtered.Where(p => p.TravelDate.HasValue &&
                    p.TravelDate.Value.Date <= end &&
                    p.TravelDate.Value.Date.AddDays(Math.Max(1, p.TravelDays) - 1) >= start);
            }

            var ids = published.Select(p => p.PostId).ToList();
            var counts = await _context.PostInteractions.AsNoTracking()
                .Where(i => ids.Contains(i.PostId)).GroupBy(i => i.PostId)
                .Select(g => new { Id = g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Id, g => g.Count);
            var ordered = scope == "recommended"
                ? filtered.OrderByDescending(p => counts.GetValueOrDefault(p.PostId)).ThenByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                : filtered.OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt);
            var totalPages = Math.Max(1, (int)Math.Ceiling(ordered.Count() / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);
            var visitorId = GetOrCreateVisitorId();
            var favorites = await _context.PostInteractions.AsNoTracking()
                .Where(i => i.MemberId == visitorId && i.ActionType == PostInteractionType.Favorite && ids.Contains(i.PostId))
                .Select(i => i.PostId).ToListAsync();
            return View(new LazyTravel.ViewModels.ExploreCardsViewModel
            {
                Articles = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                AllCountries = countries, Country = country, Scope = scope,
                StartDate = hasStart ? start.ToString("yyyy-MM-dd") : null,
                EndDate = hasEnd ? end.ToString("yyyy-MM-dd") : null,
                Page = page, TotalPages = totalPages, Favorites = favorites.ToHashSet()
            });
        }

        // GET /Explore/Create
        public async Task<IActionResult> Create()
        {
            var memberId = await GetOrCreateVisitorMemberIdAsync();
            var now = DateTime.Now;
            var post = new VlogPost
            {
                MemberId = memberId,
                Title = "未命名文章",
                Destination = "",
                Content = "",
                MediaUrl = "",
                MediaType = VlogMediaType.Photo,
                TravelDate = null,
                TravelDays = 1,
                TravelPeople = TravelGroupSize.Solo,
                Status = VlogPostStatus.Draft,
                CreatedAt = now,
                UpdatedAt = now,
                IsDelete = false,
            };
            _context.VlogPosts.Add(post);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Edit), new { id = post.PostId });
        }

        // GET /Explore/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var post = await _context.VlogPosts.AsNoTracking()
                .Include(p => p.VlogPostImages)
                .Include(p => p.ItineraryNodes)
                .FirstOrDefaultAsync(p => p.PostId == id && !p.IsDelete);
            if (post is null) return NotFound();
            if (!IsArticleOwner(post.MemberId)) return StatusCode(403);
            if (post.Status == VlogPostStatus.Published) return RedirectToAction(nameof(Details), new { id });

            var nodes = post.ItineraryNodes.OrderBy(n => n.DayNumber).ThenBy(n => n.ArrivalTime).ThenBy(n => n.NodeId).ToList();
            var images = post.VlogPostImages.Where(i => !i.IsDeleted && i.ImageType == 0)
                .OrderByDescending(i => i.IsCover)
                .ThenBy(i => i.SortOrder)
                .Select(i => i.ImageUrl)
                .Concat(post.MediaType == VlogMediaType.Photo ? new[] { post.MediaUrl } : Array.Empty<string>())
                .Where(IsImageUrl)
                .Distinct()
                .ToList();

            return View(new LazyTravel.ViewModels.ExploreEditorViewModel
            {
                Post = post,
                Source = await _context.VlogPostRoomExports.AsNoTracking().FirstOrDefaultAsync(e => e.PostId == id),
                Nodes = nodes,
                Images = images,
            });
        }
        // GET /Explore/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var post = await _context.VlogPosts.AsNoTracking().Include(p => p.Member)
                .Include(p => p.VlogPostImages).Include(p => p.ItineraryNodes)
                .FirstOrDefaultAsync(p => p.PostId == id && !p.IsDelete);
            if (post is null || (post.Status != VlogPostStatus.Published &&
                !((post.Status == VlogPostStatus.Draft || post.Status == VlogPostStatus.PendingReview) && IsArticleOwner(post.MemberId)))) return NotFound();

            var visitorId = GetOrCreateVisitorId();
            var nodes = post.ItineraryNodes.OrderBy(n => n.DayNumber).ThenBy(n => n.ArrivalTime).ThenBy(n => n.NodeId).ToList();
            var images = post.VlogPostImages.Where(i => !i.IsDeleted && i.ImageType == 0)
                .OrderByDescending(i => i.IsCover).ThenBy(i => i.SortOrder).Select(i => i.ImageUrl)
                .Concat(post.MediaType == VlogMediaType.Photo ? new[] { post.MediaUrl } : Array.Empty<string>())
                .Concat(nodes.Where(n => n.MediaType == VlogMediaType.Photo).Select(n => n.MediaUrl))
                .Where(IsImageUrl).Distinct().ToList();
            var vm = new LazyTravel.ViewModels.ExploreDetailsViewModel
            {
                Post = post, Nodes = nodes, Images = images,
                Source = await _context.VlogPostRoomExports.AsNoTracking().FirstOrDefaultAsync(e => e.PostId == id),
                PublishedCount = await _context.VlogPosts.CountAsync(p => p.MemberId == post.MemberId && !p.IsDelete && p.Status == VlogPostStatus.Published),
                CompletedCount = await _context.TravelGroups.CountAsync(g => !g.IsDelete && g.GroupStatus == 3 &&
                    (g.OwnerMemberId == post.MemberId || g.GroupMembers.Any(m => m.MemberId == post.MemberId && !m.IsRemoved))),
                LikeCount = await _context.PostInteractions.CountAsync(i => i.PostId == id && i.ActionType == PostInteractionType.Like),
                IsLiked = await _context.PostInteractions.AnyAsync(i => i.PostId == id && i.MemberId == visitorId && i.ActionType == PostInteractionType.Like),
                IsFavorited = await _context.PostInteractions.AnyAsync(i => i.PostId == id && i.MemberId == visitorId && i.ActionType == PostInteractionType.Favorite)
            };
            return View(vm);
        }

        private bool IsArticleOwner(int memberId) =>
            Request.Cookies.TryGetValue("ltvmid", out var raw) && int.TryParse(raw, out var id) && id == memberId;

        private static bool IsImageUrl(string? url) =>
            !string.IsNullOrWhiteSpace(url) && (url.StartsWith("/") && !url.StartsWith("//") ||
                Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == "https" || uri.Scheme == "http"));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            var post = await _context.VlogPosts.FirstOrDefaultAsync(p => p.PostId == id && !p.IsDelete);
            if (post is null) return NotFound();
            if (!IsArticleOwner(post.MemberId)) return StatusCode(403);
            if (post.Status != VlogPostStatus.Draft) return BadRequest("只有草稿可送出審核。");
            post.Status = VlogPostStatus.PendingReview;
            post.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["ArticleNotice"] = "文章已送出審核，通過後會出現在文章列表。";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft(int id, [FromBody] JsonElement payload)
        {
            var result = await SaveEditorAsync(id, payload, submit: false);
            return result ?? Json(new { ok = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id, [FromBody] JsonElement payload)
        {
            var result = await SaveEditorAsync(id, payload, submit: true);
            return result ?? Json(new { ok = true, redirectUrl = Url.Action(nameof(Index), "Explore") });
        }

        private async Task<IActionResult?> SaveEditorAsync(int id, JsonElement payload, bool submit)
        {
            var form = ReadArticleEditorRequest(payload);
            if (form is null) return BadRequest(new { message = "沒有收到文章資料。" });

            var post = await _context.VlogPosts
                .Include(p => p.ItineraryNodes)
                .FirstOrDefaultAsync(p => p.PostId == id && !p.IsDelete);
            if (post is null) return NotFound();
            if (!IsArticleOwner(post.MemberId)) return StatusCode(403);
            if (post.Status == VlogPostStatus.Published) return BadRequest(new { message = "已發布文章不能在此前台頁面編輯。" });

            var title = SafeText(form.Title, 100);
            if (submit && string.IsNullOrWhiteSpace(title))
                return BadRequest(new { message = "請輸入行程主題。" });

            post.Title = string.IsNullOrWhiteSpace(title) ? "未命名文章" : title;
            var intro = SafeText(form.Intro, 4000);
            var highlight = SafeText(form.Highlight, 180);
            post.Content = string.IsNullOrWhiteSpace(highlight)
                ? System.Net.WebUtility.HtmlEncode(intro)
                : $"<!--LT-HIGHLIGHT:{System.Net.WebUtility.HtmlEncode(highlight)}-->{System.Net.WebUtility.HtmlEncode(intro)}";
            post.Status = submit ? VlogPostStatus.PendingReview : VlogPostStatus.Draft;
            post.UpdatedAt = DateTime.Now;

            _context.ItineraryNodes.RemoveRange(post.ItineraryNodes);
            foreach (var day in BuildArticleNodes(form.Days))
            {
                post.ItineraryNodes.Add(day);
            }

            await _context.SaveChangesAsync();
            return null;
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
        public async Task<IActionResult> ToggleFavorite(int id, string? returnUrl)
        {
            await ToggleInteractionAsync(id, PostInteractionType.Favorite);
            if (Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl!);
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task ToggleInteractionAsync(int postId, PostInteractionType actionType)
        {
            var postExists = await _context.VlogPosts.AnyAsync(p => p.PostId == postId && !p.IsDelete && p.Status == VlogPostStatus.Published);
            if (!postExists)
            {
                return;
            }

            var visitorId = GetOrCreateVisitorId();
            var existing = await _context.PostInteractions
                .FirstOrDefaultAsync(i => i.PostId == postId && i.MemberId == visitorId && i.ActionType == actionType);

            if (existing is not null)
            {
                _context.PostInteractions.Remove(existing);
            }
            else
            {
                _context.PostInteractions.Add(new PostInteraction { PostId = postId, MemberId = visitorId, ActionType = actionType });
            }

            await _context.SaveChangesAsync();
        }

        private const string VisitorCookieName = "ltvid";
        private const string VisitorMemberCookieName = "ltvmid";

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

        private async Task<int> GetOrCreateVisitorMemberIdAsync()
        {
            if (Request.Cookies.TryGetValue(VisitorMemberCookieName, out var raw) && int.TryParse(raw, out var existingId))
            {
                var stillValid = await _context.Users.AnyAsync(m => m.Id == existingId);
                if (stillValid) return existingId;
            }

            var candidateId = await _context.Users.AsNoTracking()
                .Where(m => m.Email != LazyTravel.Shared.Models.MemberLookup.OfficialAccountEmail)
                .OrderBy(m => m.Id)
                .Select(m => m.Id)
                .FirstOrDefaultAsync();

            if (candidateId == 0)
                candidateId = await _context.Users.AsNoTracking().OrderBy(m => m.Id).Select(m => m.Id).FirstAsync();

            Response.Cookies.Append(VisitorMemberCookieName, candidateId.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(2),
                IsEssential = true,
                HttpOnly = true,
            });
            return candidateId;
        }

        private static ArticleEditorRequest? ReadArticleEditorRequest(JsonElement payload)
        {
            if (payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return null;
            try
            {
                return JsonSerializer.Deserialize<ArticleEditorRequest>(payload.GetRawText(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static IEnumerable<ItineraryNode> BuildArticleNodes(List<ArticleEditorDayRequest>? days)
        {
            if (days is null) yield break;
            for (var index = 0; index < days.Count; index++)
            {
                var day = days[index];
                var title = SafeText(day.Title, 100);
                var route = SafeText(day.Route, 4000);
                var note = SafeText(day.Note, 1000);
                if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(route) && string.IsNullOrWhiteSpace(note))
                    continue;

                yield return new ItineraryNode
                {
                    DayNumber = index + 1,
                    LocationName = string.IsNullOrWhiteSpace(title) ? $"DAY {index + 1}" : title,
                    ArrivalTime = null,
                    DepartureTime = null,
                    StayTime = null,
                    MediaUrl = "",
                    MediaType = VlogMediaType.Photo,
                    Description = route,
                    Remarks = note,
                };
            }
        }

        private static string SafeText(string? value, int maxLength)
        {
            var text = (value ?? "").Trim();
            return text.Length <= maxLength ? text : text[..maxLength];
        }

        public sealed class ArticleEditorRequest
        {
            public string? Title { get; set; }
            public string? Intro { get; set; }
            public string? Highlight { get; set; }
            public List<ArticleEditorDayRequest>? Days { get; set; }
        }

        public sealed class ArticleEditorDayRequest
        {
            public string? Title { get; set; }
            public string? Route { get; set; }
            public string? Note { get; set; }
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
