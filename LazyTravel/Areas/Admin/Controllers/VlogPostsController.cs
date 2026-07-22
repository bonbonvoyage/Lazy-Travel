using LazyTravel.Areas.Admin.Models;
using LazyTravel.Models;
using LazyTravel.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace LazyTravel.Areas.Admin.Controllers
{
    // 之後 Cookie 認證與 Role 授權建好後，改成:
    // [Authorize(Roles = "Admin,SuperAdmin")]
    [Area("Admin")]
    public class VlogPostsController : Controller
    {
        private const int PageSize = 8;
        private const long MaxUploadSizeBytes = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        private readonly IWebHostEnvironment _env;
        private readonly IAdminLogService _adminLogService;

        public VlogPostsController(IWebHostEnvironment env, IAdminLogService adminLogService)
        {
            _env = env;
            _adminLogService = adminLogService;
        }

        // GET /Admin/VlogPosts
        public async Task<IActionResult> Index(string? keyword, string? status, bool showDeleted = false, string? sort = "updated_desc", int page = 1)
        {
            var all = VlogPostStore.GetAll();

            IEnumerable<VlogPost> filtered = showDeleted
                ? all.Where(p => p.IsDelete)
                : all.Where(p => !p.IsDelete);

            if (!showDeleted && !string.IsNullOrWhiteSpace(status) && Enum.TryParse<VlogPostStatus>(status, out var statusEnum))
            {
                filtered = filtered.Where(p => p.Status == statusEnum);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                filtered = filtered.Where(p =>
                    p.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    p.Destination.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            var ordered = sort == "updated_asc"
                ? filtered.OrderBy(p => p.UpdatedAt ?? p.CreatedAt).ToList()
                : filtered.OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt).ToList();

            page = Math.Max(page, 1);
            var totalCount = ordered.Count;
            var pageItems = ordered.Skip((page - 1) * PageSize).Take(PageSize).ToList();

            var vm = new VlogPostIndexViewModel
            {
                Posts = pageItems,
                Keyword = keyword,
                Status = status,
                ShowDeleted = showDeleted,
                Sort = sort == "updated_asc" ? "updated_asc" : "updated_desc",
                Page = page,
                PageSize = PageSize,
                TotalCount = totalCount,
                TotalPosts = all.Count(p => !p.IsDelete),
                PublishedCount = all.Count(p => !p.IsDelete && p.Status == VlogPostStatus.Published),
                DraftCount = all.Count(p => !p.IsDelete && p.Status == VlogPostStatus.Draft),
                DeletedCount = all.Count(p => p.IsDelete),
            };

            var recentLogs = await _adminLogService.GetRecentAsync(50);
            vm.RecentLogs = recentLogs.Where(l => l.TargetTable == "VlogPosts").Take(20).ToList();

            ViewData["Title"] = "Vlog 行程文章";
            return View(vm);
        }

        // GET /Admin/VlogPosts/Details/5
        public IActionResult Details(int id)
        {
            var post = VlogPostStore.GetById(id);
            if (post is null)
            {
                return NotFound();
            }

            ViewData["Title"] = "文章詳情";
            ViewBag.Nodes = ItineraryNodeStore.GetByPostId(id)
                .OrderBy(n => n.DayNumber).ThenBy(n => n.ArrivalTime).ToList();
            ViewBag.LikeCount = PostInteractionStore.GetLikeCount(id);
            ViewBag.FavoriteCount = PostInteractionStore.GetFavoriteCount(id);
            return View(post);
        }

        // GET /Admin/VlogPosts/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "新增文章";
            // Cookie 認證尚未接上，先固定用 LazyTravel 官方（MemberID 4）當發文會員
            return View(new VlogPost { TravelDays = 1, MemberID = 4 });
        }

        // POST /Admin/VlogPosts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VlogPost post, IFormFile? coverFile)
        {
            if (coverFile is not null && coverFile.Length > 0 && !IsAllowedImage(coverFile))
            {
                ModelState.AddModelError(string.Empty, "封面只接受 jpg / png / gif / webp 圖片檔，且大小上限 5MB。");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "新增文章";
                return View(post);
            }

            if (coverFile is not null && coverFile.Length > 0)
            {
                post.MediaUrl = await SaveUploadedFileAsync(coverFile, "cover");
            }

            VlogPostStore.Add(post);

            await _adminLogService.WriteAsync(
                User.Identity?.Name ?? "管理員",
                "新增文章",
                $"新增文章「{post.Title}」",
                targetTable: "VlogPosts",
                targetId: post.PostID);

            TempData["SuccessMessage"] = $"文章「{post.Title}」已新增。";
            return RedirectToAction(nameof(Index));
        }

        // GET /Admin/VlogPosts/Edit/5
        // editId 有值時，下方行程表單切成「編輯行程」模式
        public IActionResult Edit(int id, int? editId = null)
        {
            var post = VlogPostStore.GetById(id);
            if (post is null)
            {
                return NotFound();
            }

            ViewData["Title"] = "編輯文章";
            return View(BuildItineraryViewModel(post, editId));
        }

        // POST /Admin/VlogPosts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VlogPost post, IFormFile? coverFile, string? existingMediaUrl)
        {
            post.PostID = id;

            if (coverFile is not null && coverFile.Length > 0 && !IsAllowedImage(coverFile))
            {
                ModelState.AddModelError(string.Empty, "封面只接受 jpg / png / gif / webp 圖片檔，且大小上限 5MB。");
            }

            if (!ModelState.IsValid)
            {
                post.MediaUrl = existingMediaUrl;
                ViewData["Title"] = "編輯文章";
                return View(BuildItineraryViewModel(post, null));
            }

            post.MediaUrl = coverFile is not null && coverFile.Length > 0
                ? await SaveUploadedFileAsync(coverFile, "cover")
                : existingMediaUrl;

            if (!VlogPostStore.Update(post))
            {
                return NotFound();
            }

            await _adminLogService.WriteAsync(
                User.Identity?.Name ?? "管理員",
                "編輯文章",
                $"編輯文章「{post.Title}」",
                targetTable: "VlogPosts",
                targetId: post.PostID);

            TempData["SuccessMessage"] = $"文章「{post.Title}」已更新。";
            return RedirectToAction(nameof(Index));
        }

        // POST /Admin/VlogPosts/Delete/5（軟刪除，設定 IsDelete = true）
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var post = VlogPostStore.GetById(id);
            if (post is null)
            {
                return NotFound();
            }

            VlogPostStore.Delete(id);

            await _adminLogService.WriteAsync(
                User.Identity?.Name ?? "管理員",
                "刪除文章",
                $"刪除文章「{post.Title}」",
                targetTable: "VlogPosts",
                targetId: post.PostID);

            TempData["SuccessMessage"] = $"文章「{post.Title}」已刪除，可在「已刪除」分頁還原。";
            return RedirectToAction(nameof(Index));
        }

        // POST /Admin/VlogPosts/Restore/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var post = VlogPostStore.GetById(id);
            if (post is null)
            {
                return NotFound();
            }

            VlogPostStore.Restore(id);

            await _adminLogService.WriteAsync(
                User.Identity?.Name ?? "管理員",
                "還原文章",
                $"還原文章「{post.Title}」",
                targetTable: "VlogPosts",
                targetId: post.PostID);

            TempData["SuccessMessage"] = $"文章「{post.Title}」已還原。";
            return RedirectToAction(nameof(Index));
        }

        // POST /Admin/VlogPosts/ToggleStatus/5（草稿 <-> 已發布 快速切換）
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, VlogPostStatus to)
        {
            var post = VlogPostStore.GetById(id);
            if (post is null)
            {
                return NotFound();
            }

            post.Status = to;
            post.UpdatedAt = DateTime.Now;

            await _adminLogService.WriteAsync(
                User.Identity?.Name ?? "管理員",
                "切換狀態",
                $"文章「{post.Title}」狀態改為「{to.ToLabel()}」",
                targetTable: "VlogPosts",
                targetId: post.PostID);

            TempData["SuccessMessage"] = $"文章「{post.Title}」已更新為「{to.ToLabel()}」。";
            return RedirectToAction(nameof(Index));
        }

        // POST /Admin/VlogPosts/UploadImage
        // 給 Quill 圖文編輯器的插圖按鈕用：上傳後回傳圖片網址，前端再把網址插進內文。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImage(IFormFile? file)
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest(new { message = "沒有收到檔案。" });
            }

            if (!IsAllowedImage(file))
            {
                return BadRequest(new { message = "只接受 jpg / png / gif / webp 圖片檔，且大小上限 5MB。" });
            }

            var url = await SaveUploadedFileAsync(file, "content");
            return Json(new { url });
        }

        // ---------- 每日行程列表管理（併入「編輯文章」頁面下方） ----------

        // 組出「編輯文章」頁面用的 ViewModel：文章本身 + 當天行程清單 + 目前正在編輯的行程節點
        private static ItineraryViewModel BuildItineraryViewModel(VlogPost post, int? editId)
        {
            var nodes = ItineraryNodeStore.GetByPostId(post.PostID)
                .OrderBy(n => n.DayNumber).ThenBy(n => n.ArrivalTime).ToList();

            var editingNode = editId.HasValue ? nodes.FirstOrDefault(n => n.NodeID == editId.Value) : null;
            var (editingHours, editingMinutes) = ParseStayTime(editingNode?.StayTime);

            return new ItineraryViewModel
            {
                Post = post,
                Nodes = nodes,
                EditingNode = editingNode,
                EditingStayHours = editingHours,
                EditingStayMinutes = editingMinutes,
            };
        }

        // POST /Admin/VlogPosts/AddNode
        // 用明確參數接快速新增表單，避免 TimeOnly 直接綁定的邊角情況
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNode(int postId, int dayNumber, string locationName, string? arrivalTime,
            int stayHours, int stayMinutes, string? departureTime, IFormFile? mediaFile,
            string? description, string? remarks)
        {
            var post = VlogPostStore.GetById(postId);
            if (post is null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(locationName))
            {
                TempData["ErrorMessage"] = "請輸入景點名稱。";
                return RedirectToAction(nameof(Edit), new { id = postId });
            }

            if (mediaFile is not null && mediaFile.Length > 0 && !IsAllowedImage(mediaFile))
            {
                TempData["ErrorMessage"] = "圖片只接受 jpg / png / gif / webp，且大小上限 5MB。";
                return RedirectToAction(nameof(Edit), new { id = postId });
            }

            string? mediaUrl = null;
            if (mediaFile is not null && mediaFile.Length > 0)
            {
                mediaUrl = await SaveUploadedFileAsync(mediaFile, "itinerary");
            }

            ItineraryNodeStore.Add(new ItineraryNode
            {
                PostID = postId,
                DayNumber = dayNumber < 1 ? 1 : dayNumber,
                LocationName = locationName,
                ArrivalTime = TimeOnly.TryParse(arrivalTime, out var at) ? at : null,
                StayTime = FormatStayTime(stayHours, stayMinutes),
                DepartureTime = TimeOnly.TryParse(departureTime, out var dt) ? dt : null,
                MediaUrl = mediaUrl,
                MediaType = VlogMediaType.Photo, // 目前只支援圖片，之後要開放影片時這裡改回接表單參數
                Description = description,
                Remarks = remarks,
            });

            TempData["SuccessMessage"] = $"已新增行程「{locationName}」。";
            return RedirectToAction(nameof(Edit), new { id = postId });
        }

        // 把「停留時間」的小時/分鐘兩個下拉選單組合成一句文字存進 StayTime(nvarchar(50))
        private static string? FormatStayTime(int hours, int minutes)
        {
            if (hours <= 0 && minutes <= 0)
            {
                return null;
            }

            var parts = new List<string>();
            if (hours > 0)
            {
                parts.Add($"{hours} 小時");
            }
            if (minutes > 0)
            {
                parts.Add($"{minutes} 分鐘");
            }

            return string.Join(" ", parts);
        }

        // FormatStayTime 的反向操作：把既有的「2 小時 30 分鐘」文字解析回小時/分鐘，供編輯表單預選下拉選單
        private static (int Hours, int Minutes) ParseStayTime(string? stayTime)
        {
            if (string.IsNullOrWhiteSpace(stayTime))
            {
                return (0, 0);
            }

            var hourMatch = System.Text.RegularExpressions.Regex.Match(stayTime, @"(\d+)\s*小時");
            var minuteMatch = System.Text.RegularExpressions.Regex.Match(stayTime, @"(\d+)\s*分鐘");

            var hours = hourMatch.Success ? int.Parse(hourMatch.Groups[1].Value) : 0;
            var minutes = minuteMatch.Success ? int.Parse(minuteMatch.Groups[1].Value) : 0;

            return (hours, minutes);
        }

        // POST /Admin/VlogPosts/UpdateNode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNode(int nodeId, int postId, int dayNumber, string locationName,
            string? arrivalTime, int stayHours, int stayMinutes, string? departureTime,
            IFormFile? mediaFile, string? existingMediaUrl, string? description, string? remarks)
        {
            var node = ItineraryNodeStore.GetById(nodeId);
            if (node is null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(locationName))
            {
                TempData["ErrorMessage"] = "請輸入景點名稱。";
                return RedirectToAction(nameof(Edit), new { id = postId, editId = nodeId });
            }

            if (mediaFile is not null && mediaFile.Length > 0 && !IsAllowedImage(mediaFile))
            {
                TempData["ErrorMessage"] = "圖片只接受 jpg / png / gif / webp，且大小上限 5MB。";
                return RedirectToAction(nameof(Edit), new { id = postId, editId = nodeId });
            }

            var mediaUrl = mediaFile is not null && mediaFile.Length > 0
                ? await SaveUploadedFileAsync(mediaFile, "itinerary")
                : existingMediaUrl;

            ItineraryNodeStore.Update(new ItineraryNode
            {
                NodeID = nodeId,
                PostID = postId,
                DayNumber = dayNumber < 1 ? 1 : dayNumber,
                LocationName = locationName,
                ArrivalTime = TimeOnly.TryParse(arrivalTime, out var at) ? at : null,
                StayTime = FormatStayTime(stayHours, stayMinutes),
                DepartureTime = TimeOnly.TryParse(departureTime, out var dt) ? dt : null,
                MediaUrl = mediaUrl,
                MediaType = VlogMediaType.Photo,
                Description = description,
                Remarks = remarks,
            });

            TempData["SuccessMessage"] = $"行程「{locationName}」已更新。";
            return RedirectToAction(nameof(Edit), new { id = postId });
        }

        // POST /Admin/VlogPosts/DeleteNode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteNode(int nodeId, int postId)
        {
            ItineraryNodeStore.Delete(nodeId);
            TempData["SuccessMessage"] = "已刪除該行程。";
            return RedirectToAction(nameof(Edit), new { id = postId });
        }

        // ---------- 檔案上傳共用小工具 ----------

        private static bool IsAllowedImage(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            return file.Length <= MaxUploadSizeBytes && AllowedImageExtensions.Contains(ext);
        }

        // 存到 wwwroot/uploads/vlog/{subFolder}/，回傳可直接放進 <img src> 的相對路徑。
        // 檔名用 GUID 避免撞名/覆蓋別人的檔案。
        private async Task<string> SaveUploadedFileAsync(IFormFile file, string subFolder)
        {
            var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "vlog", subFolder);
            Directory.CreateDirectory(uploadsRoot);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsRoot, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/vlog/{subFolder}/{fileName}";
        }
    }
}
