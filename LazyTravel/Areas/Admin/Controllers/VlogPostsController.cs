using LazyTravel.Areas.Admin.Models;
using LazyTravel.Models;
using LazyTravel.Models.EfModels;
using LazyTravel.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        private readonly LazyTravelDBContext _context;
        private readonly IAdminLogService _adminLogService;
        private readonly IReportService _reportService;

        public VlogPostsController(IWebHostEnvironment env, LazyTravelDBContext context, IAdminLogService adminLogService, IReportService reportService)
        {
            _env = env;
            _context = context;
            _adminLogService = adminLogService;
            _reportService = reportService;
        }

        // GET /Admin/VlogPosts
        // 預設一進來就是「只看我的文章」，官方文章跟一堆會員文章混在一起看起來太亂
        public async Task<IActionResult> Index(string? keyword, string? status, bool showDeleted = false, string? sort = "updated_desc", int page = 1, bool onlyMine = true)
        {
            var all = await _context.VlogPosts.AsNoTracking().ToListAsync();

            // 讚數/收藏數一次查完存字典，避免排序/分頁時對每一篇文章各自打一次資料庫。
            var interactionCounts = await _context.PostInteractions
                .GroupBy(i => new { i.PostId, i.ActionType })
                .Select(g => new { g.Key.PostId, g.Key.ActionType, Count = g.Count() })
                .ToListAsync();
            int LikeCount(int postId) => interactionCounts.FirstOrDefault(c => c.PostId == postId && c.ActionType == PostInteractionType.Like)?.Count ?? 0;
            int FavoriteCount(int postId) => interactionCounts.FirstOrDefault(c => c.PostId == postId && c.ActionType == PostInteractionType.Favorite)?.Count ?? 0;

            // 後台目前只有 LazyTravel 官方帳號能登入，這裡先用固定的官方 MemberID 篩「我的文章」；
            // 之後 Cookie 認證接上後，改成比對目前登入者的 MemberID 即可。
            // 「全部會員」現在是「我的以外的會員文章」，不是「全部」，跟「我的文章」互斥、不重疊。
            IEnumerable<VlogPost> filtered = onlyMine
                ? all.Where(p => p.MemberId == MemberLookup.OfficialMemberId)
                : all.Where(p => p.MemberId != MemberLookup.OfficialMemberId);

            filtered = showDeleted
                ? filtered.Where(p => p.IsDelete)
                : filtered.Where(p => !p.IsDelete);

            if (!showDeleted && !string.IsNullOrWhiteSpace(status) && Enum.TryParse<VlogPostStatus>(status, out var statusEnum))
            {
                filtered = filtered.Where(p => p.Status == statusEnum);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                filtered = filtered.Where(p =>
                    p.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    p.Destination.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    MemberLookup.GetName(p.MemberId).Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            // 支援的排序欄位：更新時間（預設）、讚數、收藏數，各自可以升冪/降冪
            var validSort = sort switch
            {
                "updated_asc" or "updated_desc" or
                "likes_asc" or "likes_desc" or
                "favorites_asc" or "favorites_desc" => sort,
                _ => "updated_desc",
            };

            IOrderedEnumerable<VlogPost> ordered0 = validSort switch
            {
                "updated_asc" => filtered.OrderBy(p => p.UpdatedAt ?? p.CreatedAt),
                "likes_asc" => filtered.OrderBy(p => LikeCount(p.PostId)),
                "likes_desc" => filtered.OrderByDescending(p => LikeCount(p.PostId)),
                "favorites_asc" => filtered.OrderBy(p => FavoriteCount(p.PostId)),
                "favorites_desc" => filtered.OrderByDescending(p => FavoriteCount(p.PostId)),
                _ => filtered.OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt),
            };
            var ordered = ordered0.ToList();

            page = Math.Max(page, 1);
            var totalCount = ordered.Count;
            var pageItems = ordered.Skip((page - 1) * PageSize).Take(PageSize).ToList();

            var mineBase = onlyMine
                ? all.Where(p => p.MemberId == MemberLookup.OfficialMemberId)
                : all.Where(p => p.MemberId != MemberLookup.OfficialMemberId);

            // 這一頁裡的會員文章，先各自查一次是否有待處理檢舉，讓「提出檢舉」按鈕在已經被檢舉時不要重複顯示。
            var postIdsWithPendingReport = pageItems
                .Where(p => !MemberLookup.IsOfficial(p.MemberId))
                .Where(p => _reportService.GetRelatedReports(new LazyTravel.Models.Report { Id = -1, TargetType = ReportTargetType.VlogPost, TargetId = p.PostId })
                    .Any(r => r.Status == ReportStatus.Pending))
                .Select(p => p.PostId)
                .ToHashSet();

            var vm = new VlogPostIndexViewModel
            {
                Posts = pageItems,
                Keyword = keyword,
                Status = status,
                ShowDeleted = showDeleted,
                Sort = validSort,
                Page = page,
                PageSize = PageSize,
                TotalCount = totalCount,
                OnlyMine = onlyMine,
                TotalPosts = mineBase.Count(p => !p.IsDelete),
                PublishedCount = mineBase.Count(p => !p.IsDelete && p.Status == VlogPostStatus.Published),
                DraftCount = mineBase.Count(p => !p.IsDelete && p.Status == VlogPostStatus.Draft),
                PendingReviewCount = mineBase.Count(p => !p.IsDelete && p.Status == VlogPostStatus.PendingReview),
                DeletedCount = mineBase.Count(p => p.IsDelete),
                LikeCounts = pageItems.ToDictionary(p => p.PostId, p => LikeCount(p.PostId)),
                FavoriteCounts = pageItems.ToDictionary(p => p.PostId, p => FavoriteCount(p.PostId)),
                PostIdsWithPendingReport = postIdsWithPendingReport,
            };

            var recentLogs = await _adminLogService.GetRecentAsync(50);
            vm.RecentLogs = recentLogs.Where(l => l.TargetTable == "VlogPosts").Take(20).ToList();

            ViewData["Title"] = "Vlog 行程文章";
            return View(vm);
        }

        // GET /Admin/VlogPosts/Details/5
        // 唯讀審核頁，給主管／系統管理員用；小編看不到這頁的操作按鈕。
        public async Task<IActionResult> Details(int id)
        {
            var post = await _context.VlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.PostId == id);
            if (post is null)
            {
                return NotFound();
            }

            ViewData["Title"] = "文章詳情";

            var nodes = await _context.ItineraryNodes.AsNoTracking()
                .Where(n => n.PostId == id)
                .OrderBy(n => n.DayNumber).ThenBy(n => n.ArrivalTime).ToListAsync();
            var likeCount = await _context.PostInteractions.CountAsync(i => i.PostId == id && i.ActionType == PostInteractionType.Like);
            var favoriteCount = await _context.PostInteractions.CountAsync(i => i.PostId == id && i.ActionType == PostInteractionType.Favorite);

            // 用一筆不存在資料庫的假 Report（Id=-1）借道 GetRelatedReports 撈「同一篇文章」的全部檢舉紀錄，
            // 不動檢舉中心（IReportService）任何既有邏輯，純讀取。
            var reports = _reportService.GetRelatedReports(new LazyTravel.Models.Report { Id = -1, TargetType = ReportTargetType.VlogPost, TargetId = id });
            var hasPendingReport = reports.Any(r => r.Status == ReportStatus.Pending);
            var latestJudged = reports
                .Where(r => r.Status != ReportStatus.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault();
            var reviewLog = latestJudged is not null ? await _reportService.GetReviewLogAsync(latestJudged.Id) : null;

            var vm = new VlogPostDetailsViewModel
            {
                Post = post,
                Nodes = nodes,
                LikeCount = likeCount,
                FavoriteCount = favoriteCount,
                ReportCount = reports.Count,
                ReviewStatus = post.IsDelete
                    ? "已下架"
                    : hasPendingReport
                        ? "檢舉審核中"
                        : reports.Any(r => r.Status == ReportStatus.Upheld)
                            ? "違規"
                            : "正常（無檢舉）",
                LastReviewer = reviewLog?.OperatorName,
                LastReviewedAt = reviewLog?.CreatedAt,
                LastReviewNote = latestJudged?.AdminNotes,
                Permissions = VlogPostPermissions.For(post, hasPendingReport),
            };

            return View(vm);
        }

        // GET /Admin/VlogPosts/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "新增文章";
            // Cookie 認證尚未接上，先固定用 LazyTravel 官方當發文會員
            return View(new VlogPost { TravelDays = 1, MemberId = MemberLookup.OfficialMemberId });
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

            post.CreatedAt = DateTime.Now;
            post.UpdatedAt = null;
            post.IsDelete = false;

            _context.VlogPosts.Add(post);
            await _context.SaveChangesAsync();

            await _adminLogService.WriteAsync(
                User.Identity?.Name ?? "管理員",
                "新增文章",
                $"新增文章「{post.Title}」",
                targetTable: "VlogPosts",
                targetId: post.PostId);

            TempData["SuccessMessage"] = $"文章「{post.Title}」已新增。";
            return RedirectToAction(nameof(Index));
        }

        // GET /Admin/VlogPosts/Edit/5
        // 小編編輯自己草稿用的頁面。editId 有值時，下方行程表單切成「編輯行程」模式。
        public async Task<IActionResult> Edit(int id, int? editId = null)
        {
            var post = await _context.VlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.PostId == id);
            if (post is null)
            {
                return NotFound();
            }

            ViewData["Title"] = "編輯文章";
            return View(await BuildItineraryViewModelAsync(post, editId));
        }

        // POST /Admin/VlogPosts/Edit/5
        // action="draft" 儲存草稿、action="submit" 送出審核，由 _Form.cshtml 的兩個送出按鈕決定。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VlogPost post, IFormFile? coverFile, string? existingMediaUrl, string action = "draft")
        {
            post.PostId = id;

            var existing = await _context.VlogPosts.FirstOrDefaultAsync(p => p.PostId == id);
            if (existing is null)
            {
                return NotFound();
            }

            // Edit 頁只給小編編輯「官方文章」的草稿：會員文章一律不能編輯，
            // 官方文章已送審/已發布/已刪除的話要先請主管退回草稿才能再編輯。
            // 防止有人繞過畫面上的權限判斷直接送 POST。
            if (!MemberLookup.IsOfficial(existing.MemberId) || existing.IsDelete || existing.Status != VlogPostStatus.Draft)
            {
                return Forbid();
            }

            if (coverFile is not null && coverFile.Length > 0 && !IsAllowedImage(coverFile))
            {
                ModelState.AddModelError(string.Empty, "封面只接受 jpg / png / gif / webp 圖片檔，且大小上限 5MB。");
            }

            if (!ModelState.IsValid)
            {
                post.MediaUrl = existingMediaUrl;
                ViewData["Title"] = "編輯文章";
                return View(await BuildItineraryViewModelAsync(post, null));
            }

            post.MediaUrl = coverFile is not null && coverFile.Length > 0
                ? await SaveUploadedFileAsync(coverFile, "cover")
                : existingMediaUrl;

            // 只更新允許被編輯的欄位，PostId / CreatedAt / IsDelete 不從表單覆蓋回來
            existing.MemberId = post.MemberId;
            existing.Title = post.Title;
            existing.MediaUrl = post.MediaUrl;
            existing.MediaType = post.MediaType;
            existing.Content = post.Content;
            existing.Destination = post.Destination;
            existing.TravelDays = post.TravelDays;
            existing.TravelPeople = post.TravelPeople;
            existing.TravelDate = post.TravelDate;
            existing.Status = action == "submit" ? VlogPostStatus.PendingReview : VlogPostStatus.Draft;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var isSubmit = action == "submit";
            await _adminLogService.WriteAsync(
                User.Identity?.Name ?? "小編",
                isSubmit ? "送出審核" : "儲存草稿",
                isSubmit ? $"文章「{post.Title}」已送出審核" : $"編輯文章「{post.Title}」",
                targetTable: "VlogPosts",
                targetId: post.PostId);

            TempData["SuccessMessage"] = isSubmit ? $"文章「{post.Title}」已送出審核。" : $"文章「{post.Title}」已儲存為草稿。";
            return RedirectToAction(nameof(Index));
        }

        // POST /Admin/VlogPosts/Delete/5（軟刪除，設定 IsDelete = true）
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.VlogPosts.FirstOrDefaultAsync(p => p.PostId == id);
            if (post is null)
            {
                return NotFound();
            }

            post.IsDelete = true;
            post.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            await _adminLogService.WriteAsync(
                User.Identity?.Name ?? "管理員",
                "刪除文章",
                $"刪除文章「{post.Title}」",
                targetTable: "VlogPosts",
                targetId: post.PostId);

            TempData["SuccessMessage"] = $"文章「{post.Title}」已刪除，可在「已刪除」分頁還原。";
            return RedirectToAction(nameof(Index));
        }

        // POST /Admin/VlogPosts/Restore/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var post = await _context.VlogPosts.FirstOrDefaultAsync(p => p.PostId == id);
            if (post is null)
            {
                return NotFound();
            }

            post.IsDelete = false;
            post.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            await _adminLogService.WriteAsync(
                User.Identity?.Name ?? "管理員",
                "還原文章",
                $"還原文章「{post.Title}」",
                targetTable: "VlogPosts",
                targetId: post.PostId);

            TempData["SuccessMessage"] = $"文章「{post.Title}」已還原。";
            return RedirectToAction(nameof(Index));
        }

        // POST /Admin/VlogPosts/ToggleStatus/5（草稿 <-> 已發布 快速切換；主管在檢視頁按「審核通過」「退回草稿」也是走這裡）
        // 退回草稿一定要附備註：小編才知道要改哪裡，備註存進 AdminLogs 的 Detail 欄位，不用另外開欄位/資料表，
        // Edit 頁再把最近一次的「退回草稿」紀錄撈出來顯示給小編看。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, VlogPostStatus to, string? note = null, bool fromDetails = false)
        {
            var post = await _context.VlogPosts.FirstOrDefaultAsync(p => p.PostId == id);
            if (post is null)
            {
                return NotFound();
            }

            // 草稿/送審/已發布這條流程只適用官方文章：會員文章不是小編寫的，沒有送審/退回草稿這件事。
            if (post.IsDelete || !MemberLookup.IsOfficial(post.MemberId))
            {
                return Forbid();
            }

            // 只允許「送審」(草稿→送審)、「審核通過」(送審→已發布)、「退回草稿」(任何狀態→草稿) 這三種合法轉換，
            // 防止繞過畫面直接送出其他組合。
            var validTransition =
                (post.Status == VlogPostStatus.Draft && to == VlogPostStatus.PendingReview) ||
                (post.Status == VlogPostStatus.PendingReview && to == VlogPostStatus.Published) ||
                to == VlogPostStatus.Draft;
            if (!validTransition)
            {
                return Forbid();
            }

            if (to == VlogPostStatus.Draft && string.IsNullOrWhiteSpace(note))
            {
                TempData["ErrorMessage"] = "退回草稿前請先填寫原因，讓小編知道要修改哪裡。";
                return RedirectToAction(nameof(Details), new { id });
            }

            post.Status = to;
            post.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            var isReturnToDraft = to == VlogPostStatus.Draft;
            var action = isReturnToDraft ? "退回草稿"
                : to == VlogPostStatus.Published ? "審核通過"
                : to == VlogPostStatus.PendingReview ? "送出審核"
                : "切換狀態";
            var detail = isReturnToDraft
                ? $"文章「{post.Title}」退回草稿。原因：{note}"
                : $"文章「{post.Title}」狀態改為「{to.ToLabel()}」";

            await _adminLogService.WriteAsync(
                User.Identity?.Name ?? "管理員",
                action,
                detail,
                targetTable: "VlogPosts",
                targetId: post.PostId);

            TempData["SuccessMessage"] = $"文章「{post.Title}」已更新為「{to.ToLabel()}」。";
            return fromDetails ? RedirectToAction(nameof(Details), new { id }) : RedirectToAction(nameof(Index));
        }

        // POST /Admin/VlogPosts/ReportPost/5（小編對會員文章提出檢舉；官方文章不走這裡，是自己人不用檢舉自己）
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportPost(int id, ReportReasonCategory reasonCategory, string reason)
        {
            var post = await _context.VlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.PostId == id);
            if (post is null)
            {
                return NotFound();
            }

            if (MemberLookup.IsOfficial(post.MemberId))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "請填寫檢舉原因。";
                return RedirectToAction(nameof(Details), new { id });
            }

            await _reportService.SubmitAsync(
                ReportTargetType.VlogPost,
                post.PostId,
                post.Title,
                MemberLookup.GetName(post.MemberId),
                User.Identity?.Name ?? "小編",
                reasonCategory,
                reason);

            TempData["SuccessMessage"] = $"已對文章「{post.Title}」提出檢舉，主管會盡快處理。";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST /Admin/VlogPosts/DismissReport/5（主管判定會員文章的檢舉不成立；官方文章走「退回草稿」，不走這裡）
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DismissReport(int id)
        {
            var post = await _context.VlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.PostId == id);
            if (post is null)
            {
                return NotFound();
            }

            if (MemberLookup.IsOfficial(post.MemberId))
            {
                return Forbid();
            }

            var reports = _reportService.GetRelatedReports(new LazyTravel.Models.Report { Id = -1, TargetType = ReportTargetType.VlogPost, TargetId = id });
            var pendingReport = reports.FirstOrDefault(r => r.Status == ReportStatus.Pending);
            if (pendingReport is null)
            {
                TempData["ErrorMessage"] = "目前沒有待處理的檢舉。";
                return RedirectToAction(nameof(Details), new { id });
            }

            var reviewerName = User.Identity?.Name ?? _reportService.GetRandomReviewerAlias();
            await _reportService.JudgeAsync(pendingReport.Id, ReportStatus.Dismissed, "於 Vlog 行程文章後台判定不成立", isMalicious: false, reviewerName);

            TempData["SuccessMessage"] = $"文章「{post.Title}」的檢舉已判定為不成立。";
            return RedirectToAction(nameof(Details), new { id });
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
        private async Task<ItineraryViewModel> BuildItineraryViewModelAsync(VlogPost post, int? editId)
        {
            var nodes = await _context.ItineraryNodes.AsNoTracking()
                .Where(n => n.PostId == post.PostId)
                .OrderBy(n => n.DayNumber).ThenBy(n => n.ArrivalTime).ToListAsync();

            var editingNode = editId.HasValue ? nodes.FirstOrDefault(n => n.NodeId == editId.Value) : null;
            var (editingHours, editingMinutes) = ParseStayTime(editingNode?.StayTime);

            var logs = await _adminLogService.GetForTargetAsync("VlogPosts", post.PostId);
            var lastReturn = logs.Where(l => l.Action == "退回草稿").OrderByDescending(l => l.CreatedAt).FirstOrDefault();

            return new ItineraryViewModel
            {
                Post = post,
                Nodes = nodes,
                EditingNode = editingNode,
                EditingStayHours = editingHours,
                EditingStayMinutes = editingMinutes,
                Permissions = VlogPostPermissions.For(post, hasPendingReport: false),
                LastReturnNote = lastReturn?.Detail,
                LastReturnedAt = lastReturn?.CreatedAt,
            };
        }

        // POST /Admin/VlogPosts/AddNode
        // 用明確參數接快速新增表單，避免 TimeOnly 直接綁定的邊角情況
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNode(int postId, int dayNumber, string locationName, string? arrivalTime,
            string? stayDuration, string? departureTime, IFormFile? mediaFile,
            string? description, string? remarks)
        {
            var parentPost = await _context.VlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.PostId == postId);
            if (parentPost is null)
            {
                return NotFound();
            }

            if (!MemberLookup.IsOfficial(parentPost.MemberId) || parentPost.IsDelete || parentPost.Status != VlogPostStatus.Draft)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(locationName))
            {
                TempData["ErrorMessage"] = "請輸入景點名稱。";
                return RedirectToAction(nameof(Edit), null, new { id = postId }, "itinerary-section");
            }

            if (mediaFile is not null && mediaFile.Length > 0 && !IsAllowedImage(mediaFile))
            {
                TempData["ErrorMessage"] = "圖片只接受 jpg / png / gif / webp，且大小上限 5MB。";
                return RedirectToAction(nameof(Edit), null, new { id = postId }, "itinerary-section");
            }

            string? mediaUrl = null;
            if (mediaFile is not null && mediaFile.Length > 0)
            {
                mediaUrl = await SaveUploadedFileAsync(mediaFile, "itinerary");
            }

            _context.ItineraryNodes.Add(new ItineraryNode
            {
                PostId = postId,
                DayNumber = dayNumber < 1 ? 1 : dayNumber,
                LocationName = locationName,
                ArrivalTime = TimeOnly.TryParse(arrivalTime, out var at) ? at : null,
                StayTime = FormatStayTimeFromDuration(stayDuration),
                DepartureTime = TimeOnly.TryParse(departureTime, out var dt) ? dt : null,
                MediaUrl = mediaUrl,
                MediaType = VlogMediaType.Photo, // 目前只支援圖片，之後要開放影片時這裡改回接表單參數
                Description = description,
                Remarks = remarks,
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"已新增行程「{locationName}」。";
            return RedirectToAction(nameof(Edit), null, new { id = postId }, "itinerary-section");
        }

        // 「停留時間」表單欄位現在是 <input type="time">，借用時鐘格式(HH:mm)表示「持續多久」而不是「幾點幾分」。
        // 例如輸入 02:35 代表停留 2 小時 35 分鐘，跟抵達/離開時間欄位是同一種輸入元件，操作起來一致。
        private static string? FormatStayTimeFromDuration(string? stayDuration)
        {
            if (string.IsNullOrWhiteSpace(stayDuration))
            {
                return null;
            }

            var parts = stayDuration.Split(':');
            var hours = parts.Length > 0 && int.TryParse(parts[0], out var h) ? h : 0;
            var minutes = parts.Length > 1 && int.TryParse(parts[1], out var m) ? m : 0;
            return FormatStayTime(hours, minutes);
        }

        // 把「停留時間」的小時/分鐘組合成一句文字存進 StayTime(nvarchar(50))
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

        // FormatStayTime 的反向操作：把既有的文字解析回小時/分鐘，供編輯表單預填時間欄位。
        // 假資料裡有些筆數是「1.5 小時」這種小數格式（不是「1 小時 30 分鐘」），要先特別處理，
        // 不然下面 (\d+)小時 的規則會把小數點後的數字誤判成小時數（1.5 小時 → 誤解成 5 小時）。
        private static (int Hours, int Minutes) ParseStayTime(string? stayTime)
        {
            if (string.IsNullOrWhiteSpace(stayTime))
            {
                return (0, 0);
            }

            var decimalMatch = System.Text.RegularExpressions.Regex.Match(stayTime.Trim(), @"^(\d+(?:\.\d+)?)\s*小時$");
            if (decimalMatch.Success && double.TryParse(decimalMatch.Groups[1].Value, out var decimalHours))
            {
                var totalMinutes = (int)Math.Round(decimalHours * 60);
                return (totalMinutes / 60, totalMinutes % 60);
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
            string? arrivalTime, string? stayDuration, string? departureTime,
            IFormFile? mediaFile, string? existingMediaUrl, string? description, string? remarks)
        {
            var node = await _context.ItineraryNodes.FirstOrDefaultAsync(n => n.NodeId == nodeId);
            if (node is null)
            {
                return NotFound();
            }

            var parentPost = await _context.VlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.PostId == postId);
            if (parentPost is null)
            {
                return NotFound();
            }

            if (!MemberLookup.IsOfficial(parentPost.MemberId) || parentPost.IsDelete || parentPost.Status != VlogPostStatus.Draft)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(locationName))
            {
                TempData["ErrorMessage"] = "請輸入景點名稱。";
                return RedirectToAction(nameof(Edit), null, new { id = postId, editId = nodeId }, "itinerary-section");
            }

            if (mediaFile is not null && mediaFile.Length > 0 && !IsAllowedImage(mediaFile))
            {
                TempData["ErrorMessage"] = "圖片只接受 jpg / png / gif / webp，且大小上限 5MB。";
                return RedirectToAction(nameof(Edit), null, new { id = postId, editId = nodeId }, "itinerary-section");
            }

            var mediaUrl = mediaFile is not null && mediaFile.Length > 0
                ? await SaveUploadedFileAsync(mediaFile, "itinerary")
                : existingMediaUrl;

            node.DayNumber = dayNumber < 1 ? 1 : dayNumber;
            node.LocationName = locationName;
            node.ArrivalTime = TimeOnly.TryParse(arrivalTime, out var at) ? at : null;
            node.StayTime = FormatStayTimeFromDuration(stayDuration);
            node.DepartureTime = TimeOnly.TryParse(departureTime, out var dt) ? dt : null;
            node.MediaUrl = mediaUrl;
            node.MediaType = VlogMediaType.Photo;
            node.Description = description;
            node.Remarks = remarks;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"行程「{locationName}」已更新。";
            return RedirectToAction(nameof(Edit), null, new { id = postId }, "itinerary-section");
        }

        // POST /Admin/VlogPosts/DeleteNode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNode(int nodeId, int postId)
        {
            var parentPost = await _context.VlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.PostId == postId);
            if (parentPost is null)
            {
                return NotFound();
            }

            if (!MemberLookup.IsOfficial(parentPost.MemberId) || parentPost.IsDelete || parentPost.Status != VlogPostStatus.Draft)
            {
                return Forbid();
            }

            var node = await _context.ItineraryNodes.FirstOrDefaultAsync(n => n.NodeId == nodeId);
            if (node is not null)
            {
                _context.ItineraryNodes.Remove(node);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "已刪除該行程。";
            return RedirectToAction(nameof(Edit), null, new { id = postId }, "itinerary-section");
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
