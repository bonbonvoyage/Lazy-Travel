using LazyTravel.Shared.Models;
using LazyTravel.Shared.Models.EfModels;
using LazyTravel.Shared.ViewModels;
using LazyTravel.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LazyTravel.Controllers
{
    // 前台「找行程」頁面：只給看已發布、未刪除的文章，跟後台管理邏輯分開。
    public class ExploreController : Controller
    {
        private readonly LazyTravelDBContext _context;
        private readonly ICurrentMemberAccessor _currentMemberAccessor;
        private readonly VlogPostImageUploadService _imageUploadService;

        public ExploreController(
            LazyTravelDBContext context,
            ICurrentMemberAccessor currentMemberAccessor,
            VlogPostImageUploadService imageUploadService)
        {
            _context = context;
            _currentMemberAccessor = currentMemberAccessor;
            _imageUploadService = imageUploadService;
        }

        // GET /Explore
        public async Task<IActionResult> Index(string? country, string? startDate, string? endDate, string scope = "all", int take = 12)
        {
            const int pageSize = 12;
            take = Math.Clamp(take, pageSize, 120);
            scope = scope == "mine" ? "mine" : "all";
            var currentMemberId = GetCurrentMemberId();
            var published = await _context.VlogPosts.AsNoTracking()
                .Include(p => p.Member)
                .Where(p => !p.IsDelete && p.Status == VlogPostStatus.Published)
                .Where(p => scope != "mine" || (currentMemberId.HasValue && p.MemberId == currentMemberId.Value))
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
            var interactionCounts = await _context.PostInteractions.AsNoTracking()
                .Where(i => ids.Contains(i.PostId))
                .GroupBy(i => new { i.PostId, i.ActionType })
                .Select(g => new { g.Key.PostId, g.Key.ActionType, Count = g.Count() })
                .ToListAsync();
            var favoriteCounts = interactionCounts
                .Where(i => i.ActionType == PostInteractionType.Favorite)
                .ToDictionary(i => i.PostId, i => i.Count);
            var likeCounts = interactionCounts
                .Where(i => i.ActionType == PostInteractionType.Like)
                .ToDictionary(i => i.PostId, i => i.Count);
            var ordered = filtered.OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt);
            var totalCount = ordered.Count();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            var visitorId = currentMemberId ?? await GetOrCreateVisitorMemberIdAsync();
            var favorites = await _context.PostInteractions.AsNoTracking()
                .Where(i => i.MemberId == visitorId && i.ActionType == PostInteractionType.Favorite && ids.Contains(i.PostId))
                .Select(i => i.PostId).ToListAsync();
            var likes = await _context.PostInteractions.AsNoTracking()
                .Where(i => i.MemberId == visitorId && i.ActionType == PostInteractionType.Like && ids.Contains(i.PostId))
                .Select(i => i.PostId).ToListAsync();
            return View(new LazyTravel.ViewModels.ExploreCardsViewModel
            {
                Articles = ordered.Take(take).ToList(),
                AllCountries = countries, Country = country, Scope = scope,
                StartDate = hasStart ? start.ToString("yyyy-MM-dd") : null,
                EndDate = hasEnd ? end.ToString("yyyy-MM-dd") : null,
                Page = 1, TotalPages = totalPages, TotalCount = totalCount, Take = take, Favorites = favorites.ToHashSet(), Likes = likes.ToHashSet(), FavoriteCounts = favoriteCounts, LikeCounts = likeCounts
            });
        }

        // GET /Explore/Create
        public async Task<IActionResult> Create()
        {
            var memberId = GetCurrentMemberId();
            if (!memberId.HasValue) return Unauthorized();
            var now = DateTime.Now;
            var post = new VlogPost
            {
                MemberId = memberId.Value,
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
        public async Task<IActionResult> Edit(int id, int? viewerMemberId)
        {
            var post = await _context.VlogPosts.AsNoTracking()
                .Include(p => p.VlogPostImages)
                .Include(p => p.ItineraryNodes)
                .FirstOrDefaultAsync(p => p.PostId == id && !p.IsDelete);
            if (post is null) return NotFound();
            if (!IsArticleOwner(post.MemberId, viewerMemberId)) return StatusCode(403);

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

            var visitorId = GetCurrentMemberId() ?? await GetOrCreateVisitorMemberIdAsync();
            var nodes = post.ItineraryNodes.OrderBy(n => n.DayNumber).ThenBy(n => n.ArrivalTime).ThenBy(n => n.NodeId).ToList();
            var images = post.VlogPostImages.Where(i => !i.IsDeleted && i.ImageType == 0)
                .OrderByDescending(i => i.IsCover).ThenBy(i => i.SortOrder).Select(i => i.ImageUrl)
                .Concat(post.MediaType == VlogMediaType.Photo ? new[] { post.MediaUrl } : Array.Empty<string>())
                .Concat(nodes.Where(n => n.MediaType == VlogMediaType.Photo).Select(n => n.MediaUrl))
                .Where(IsImageUrl).Distinct().ToList();
            var vm = new LazyTravel.ViewModels.ExploreDetailsViewModel
            {
                Post = post, Nodes = nodes, Images = images,
                PublishedCount = await _context.VlogPosts.CountAsync(p => p.MemberId == post.MemberId && !p.IsDelete && p.Status == VlogPostStatus.Published),
                CompletedCount = await _context.TravelGroups.CountAsync(g => !g.IsDelete && g.GroupStatus == 3 &&
                    (g.OwnerMemberId == post.MemberId || g.GroupMembers.Any(m => m.MemberId == post.MemberId && !m.IsRemoved))),
                LikeCount = await _context.PostInteractions.CountAsync(i => i.PostId == id && i.ActionType == PostInteractionType.Like),
                IsLiked = await _context.PostInteractions.AnyAsync(i => i.PostId == id && i.MemberId == visitorId && i.ActionType == PostInteractionType.Like),
                IsFavorited = await _context.PostInteractions.AnyAsync(i => i.PostId == id && i.MemberId == visitorId && i.ActionType == PostInteractionType.Favorite)
            };
            ViewBag.IsArticleOwner = IsArticleOwner(post.MemberId);
            return View(vm);
        }

        private int? GetCurrentMemberId() => _currentMemberAccessor.GetCurrentMemberId();

        private bool IsArticleOwner(int memberId, int? viewerMemberId = null)
        {
            var currentMemberId = GetCurrentMemberId();
            if (currentMemberId.HasValue) return currentMemberId.Value == memberId;

            return viewerMemberId == memberId ||
                Request.Cookies.TryGetValue("ltvmid", out var raw) && int.TryParse(raw, out var id) && id == memberId;
        }

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
            if (post.Status != VlogPostStatus.Draft) return BadRequest("只有草稿可送出。");
            post.Status = VlogPostStatus.Published;
            post.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["ArticleNotice"] = "文章已送出，已顯示在行程文章首頁。";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id, int? viewerMemberId)
        {
            if (!Request.HasFormContentType) return BadRequest(new { message = "沒有收到文章資料。" });

            var formData = await Request.ReadFormAsync();
            var payloadText = formData["payload"].ToString();
            if (string.IsNullOrWhiteSpace(payloadText)) return BadRequest(new { message = "沒有收到文章資料。" });

            using var payload = JsonDocument.Parse(payloadText);
            var wasPublished = await _context.VlogPosts.AsNoTracking()
                .AnyAsync(p => p.PostId == id && !p.IsDelete && p.Status == VlogPostStatus.Published);
            var result = await SaveEditorAsync(id, payload.RootElement, submit: true, viewerMemberId, formData.Files);
            if (result is not null) return result;

            var redirectUrl = wasPublished
                ? Url.Action(nameof(Details), "Explore", new { id, viewerMemberId })
                : Url.Action(nameof(Index), "Explore");
            return Json(new { ok = true, message = wasPublished ? "文章已更新。" : "文章已送出。", redirectUrl });
        }

        private async Task<IActionResult?> SaveEditorAsync(int id, JsonElement payload, bool submit, int? viewerMemberId = null, IFormFileCollection? files = null)
        {
            var form = ReadArticleEditorRequest(payload);
            if (form is null) return BadRequest(new { message = "沒有收到文章資料。" });

            var post = await _context.VlogPosts
                .Include(p => p.ItineraryNodes)
                .Include(p => p.VlogPostImages)
                .FirstOrDefaultAsync(p => p.PostId == id && !p.IsDelete);
            if (post is null) return NotFound();
            if (!IsArticleOwner(post.MemberId, viewerMemberId)) return StatusCode(403);

            var title = SafeText(form.Title, 100);
            if (submit && string.IsNullOrWhiteSpace(title))
                return BadRequest(new { message = "請輸入行程主題。" });

            post.Title = string.IsNullOrWhiteSpace(title) ? "未命名文章" : title;
            var intro = SafeText(form.Intro, 4000);
            var highlight = SafeText(form.Highlight, 180);
            var region = SafeText(form.Region, 100);
            post.Destination = string.IsNullOrWhiteSpace(form.Country) ? "未提供" : SafeText(form.Country, 100);
            if (Enum.TryParse<TravelGroupSize>(form.People, out var people))
            {
                post.TravelPeople = people;
            }
            ApplyTravelDate(post, form.StartDate, form.EndDate);

            var meta = "";
            if (!string.IsNullOrWhiteSpace(highlight))
                meta += $"<!--LT-HIGHLIGHT:{System.Net.WebUtility.HtmlEncode(highlight)}-->";
            if (!string.IsNullOrWhiteSpace(region))
                meta += $"<!--LT-REGION:{System.Net.WebUtility.HtmlEncode(region)}-->";
            post.Content = meta + System.Net.WebUtility.HtmlEncode(intro);
            post.Status = submit ? VlogPostStatus.Published : VlogPostStatus.Draft;
            post.UpdatedAt = DateTime.Now;

            _context.ItineraryNodes.RemoveRange(post.ItineraryNodes);
            foreach (var day in BuildArticleNodes(form.Days))
            {
                post.ItineraryNodes.Add(day);
            }

            var imageResult = await SaveArticleImagesAsync(post, files);
            if (imageResult is not null) return imageResult;

            await _context.SaveChangesAsync();
            return null;
        }

        private async Task<IActionResult?> SaveArticleImagesAsync(VlogPost post, IFormFileCollection? files)
        {
            if (files is null || files.Count == 0) return null;

            var memberId = GetCurrentMemberId();
            var now = DateTime.Now;
            var coverFile = files.GetFile("coverImage");
            if (coverFile is not null && coverFile.Length > 0)
            {
                if (!IsSupportedImageFile(coverFile)) return BadRequest(new { message = "封面圖片僅接受 PNG 或 JPG。" });

                foreach (var oldCover in post.VlogPostImages.Where(i => !i.IsDeleted && i.IsCover))
                {
                    oldCover.IsDeleted = true;
                    oldCover.UpdatedAt = now;
                }

                var url = await _imageUploadService.UploadAsync(coverFile, "cover");
                post.MediaUrl = url;
                post.MediaType = VlogMediaType.Photo;
                post.VlogPostImages.Add(new VlogPostImage
                {
                    ImageUrl = url,
                    ImageType = 0,
                    AltText = post.Title,
                    SortOrder = 0,
                    IsCover = true,
                    IsDeleted = false,
                    UploadedByMemberId = memberId,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }

            var supportFiles = files.GetFiles("supportImages").Where(f => f.Length > 0).ToList();
            if (supportFiles.Count == 0) return null;

            var nextSort = post.VlogPostImages
                .Where(i => !i.IsDeleted && !i.IsCover)
                .Select(i => i.SortOrder)
                .DefaultIfEmpty(0)
                .Max() + 1;

            foreach (var file in supportFiles)
            {
                if (!IsSupportedImageFile(file)) return BadRequest(new { message = "補充照片僅接受 PNG 或 JPG。" });

                var url = await _imageUploadService.UploadAsync(file, "itinerary");
                post.VlogPostImages.Add(new VlogPostImage
                {
                    ImageUrl = url,
                    ImageType = 0,
                    AltText = post.Title,
                    SortOrder = nextSort++,
                    IsCover = false,
                    IsDeleted = false,
                    UploadedByMemberId = memberId,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }

            return null;
        }

        private static bool IsSupportedImageFile(IFormFile file)
        {
            var contentType = file.ContentType?.ToLowerInvariant();
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            return (contentType == "image/png" || contentType == "image/jpeg" || contentType == "image/jpg") &&
                (ext == ".png" || ext == ".jpg" || ext == ".jpeg");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int? viewerMemberId)
        {
            var post = await _context.VlogPosts.FirstOrDefaultAsync(p => p.PostId == id && !p.IsDelete);
            if (post is null) return NotFound();
            if (!IsArticleOwner(post.MemberId, viewerMemberId)) return StatusCode(403);

            post.IsDelete = true;
            post.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                return Json(new { ok = true, redirectUrl = Url.Action(nameof(Index), "Explore") });

            TempData["ArticleNotice"] = "文章已刪除。";
            return RedirectToAction(nameof(Index));
        }

        // POST /Explore/ToggleLike/5 —— 讀者點一次讚就加一筆互動紀錄，再點一次取消（跟收藏共用同一套邏輯）。
        // 目前沒有真的會員登入系統，先用瀏覽器 Cookie 記一個匿名訪客 ID，同一支瀏覽器重複點才會正確切換讚/取消讚。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var active = await ToggleInteractionAsync(id, PostInteractionType.Like);
            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                var count = await _context.PostInteractions.CountAsync(i => i.PostId == id && i.ActionType == PostInteractionType.Like);
                return Json(new { ok = true, active, count });
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST /Explore/ToggleFavorite/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int id, string? returnUrl)
        {
            var active = await ToggleInteractionAsync(id, PostInteractionType.Favorite);
            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                var count = await _context.PostInteractions.CountAsync(i => i.PostId == id && i.ActionType == PostInteractionType.Favorite);
                return Json(new { ok = true, active, count });
            }
            if (Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl!);
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<bool> ToggleInteractionAsync(int postId, PostInteractionType actionType)
        {
            var postExists = await _context.VlogPosts.AnyAsync(p => p.PostId == postId && !p.IsDelete && p.Status == VlogPostStatus.Published);
            if (!postExists)
            {
                return false;
            }

            var visitorId = GetCurrentMemberId() ?? await GetOrCreateVisitorMemberIdAsync();
            var existing = await _context.PostInteractions
                .FirstOrDefaultAsync(i => i.PostId == postId && i.MemberId == visitorId && i.ActionType == actionType);

            if (existing is not null)
            {
                _context.PostInteractions.Remove(existing);
                await _context.SaveChangesAsync();
                return false;
            }
            else
            {
                _context.PostInteractions.Add(new PostInteraction { PostId = postId, MemberId = visitorId, ActionType = actionType, CreatedAt = DateTime.Now });
                await _context.SaveChangesAsync();
                return true;
            }
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

        private static void ApplyTravelDate(VlogPost post, string? startDate, string? endDate)
        {
            var hasStart = DateTime.TryParseExact(startDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var start);
            var hasEnd = DateTime.TryParseExact(endDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var end);

            if (!hasStart && !hasEnd)
            {
                post.TravelDate = null;
                post.TravelDays = Math.Max(1, post.TravelDays);
                return;
            }

            if (hasStart && !hasEnd) end = start;
            if (hasEnd && !hasStart) start = end;
            if (start > end) (start, end) = (end, start);

            post.TravelDate = start;
            post.TravelDays = Math.Max(1, (end.Date - start.Date).Days + 1);
        }

        private static IEnumerable<ItineraryNode> BuildArticleNodes(List<ArticleEditorDayRequest>? days)
        {
            if (days is null) yield break;
            for (var index = 0; index < days.Count; index++)
            {
                var day = days[index];
                var title = SafeText(day.Title, 100);
                var morning = SafeText(day.Morning, 1000);
                var afternoon = SafeText(day.Afternoon, 1000);
                var evening = SafeText(day.Evening, 1000);
                var note = SafeText(day.Note, 1000);
                if (string.IsNullOrWhiteSpace(title) &&
                    string.IsNullOrWhiteSpace(morning) &&
                    string.IsNullOrWhiteSpace(afternoon) &&
                    string.IsNullOrWhiteSpace(evening) &&
                    string.IsNullOrWhiteSpace(note))
                    continue;

                if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(note))
                {
                    yield return new ItineraryNode
                    {
                        DayNumber = index + 1,
                        LocationName = string.IsNullOrWhiteSpace(title) ? $"DAY {index + 1}" : title,
                        ArrivalTime = null,
                        DepartureTime = null,
                        StayTime = null,
                        MediaUrl = "",
                        MediaType = VlogMediaType.Photo,
                        Description = "",
                        Remarks = note,
                    };
                }

                foreach (var node in BuildPeriodNodes(index + 1, morning, afternoon, evening))
                {
                    yield return node;
                }
            }
        }

        private static IEnumerable<ItineraryNode> BuildPeriodNodes(int dayNumber, string morning, string afternoon, string evening)
        {
            foreach (var item in new[]
            {
                new { Text = morning, Time = new TimeOnly(9, 0) },
                new { Text = afternoon, Time = new TimeOnly(13, 0) },
                new { Text = evening, Time = new TimeOnly(19, 0) },
            })
            {
                if (string.IsNullOrWhiteSpace(item.Text)) continue;

                yield return new ItineraryNode
                {
                    DayNumber = dayNumber,
                    LocationName = item.Text,
                    ArrivalTime = item.Time,
                    DepartureTime = null,
                    StayTime = null,
                    MediaUrl = "",
                    MediaType = VlogMediaType.Photo,
                    Description = "",
                    Remarks = "",
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
            public string? Country { get; set; }
            public string? Region { get; set; }
            public string? StartDate { get; set; }
            public string? EndDate { get; set; }
            public string? People { get; set; }
            public List<ArticleEditorDayRequest>? Days { get; set; }
        }

        public sealed class ArticleEditorDayRequest
        {
            public string? Title { get; set; }
            public string? Morning { get; set; }
            public string? Afternoon { get; set; }
            public string? Evening { get; set; }
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












