using LazyTravel.Areas.Admin.Models;
using LazyTravel.Shared.Models;
using LazyTravel.Shared.Models.EfModels;
using LazyTravel.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

        // 官方帳號的 MemberID 查一次就快取起來，不用每個請求都查一次資料庫。
        private static int? _cachedOfficialMemberId;

        private readonly LazyTravelDBContext _context;
        private readonly IAdminLogService _adminLogService;
        private readonly IReportService _reportService;
        private readonly VlogPostImageUploadService _imageUploadService;

        public VlogPostsController(LazyTravelDBContext context, IAdminLogService adminLogService,
            IReportService reportService, VlogPostImageUploadService imageUploadService)
        {
            _context = context;
            _adminLogService = adminLogService;
            _reportService = reportService;
            _imageUploadService = imageUploadService;
        }

        // 操作紀錄/檢舉判定一律要用真正登入員工的 EmployeeID(Admin/AuthController 登入時寫進 ClaimTypes.NameIdentifier),
        // 這裡集中解析,拿不到就丟例外——沒登入不應該能走到任何會寫 AdminAuditLogs 的動作(Controller 上都已經有 [Authorize])
        private int GetCurrentEmployeeId()
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int employeeId))
            {
                throw new InvalidOperationException("無法識別目前登入的員工身分，請重新登入後台。");
            }
            return employeeId;
        }

        // GET /Admin/VlogPosts
        // 預設一進來是「會員文章」
        public async Task<IActionResult> Index(string? keyword, string? status, bool showDeleted = false, string? sort = "updated_desc", int page = 1, bool onlyMine = false)
        {
            var all = await _context.VlogPosts.AsNoTracking().Include(p => p.Member).ToListAsync();

            // 讚數/收藏數一次查完存字典，避免排序/分頁時對每一篇文章各自打一次資料庫。
            var interactionCounts = await _context.PostInteractions
                .GroupBy(i => new { i.PostId, i.ActionType })
                .Select(g => new { g.Key.PostId, g.Key.ActionType, Count = g.Count() })
                .ToListAsync();
            int LikeCount(int postId) => interactionCounts.FirstOrDefault(c => c.PostId == postId && c.ActionType == PostInteractionType.Like)?.Count ?? 0;
            int FavoriteCount(int postId) => interactionCounts.FirstOrDefault(c => c.PostId == postId && c.ActionType == PostInteractionType.Favorite)?.Count ?? 0;

            // 後台目前只有 LazyTravel 官方帳號能登入，這裡先用官方帳號的 Email 篩「我的文章」；
            // 之後 Cookie 認證接上後，改成比對目前登入者的 MemberID 即可。
            // 「會員文章」是「官方以外的會員文章」，跟「官方文章」互斥、不重疊。
            IEnumerable<VlogPost> filtered = onlyMine
                ? all.Where(p => MemberLookup.IsOfficial(p.Member))
                : all.Where(p => !MemberLookup.IsOfficial(p.Member));

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
                    (p.Member?.Name ?? string.Empty).Contains(keyword, StringComparison.OrdinalIgnoreCase));
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
                ? all.Where(p => MemberLookup.IsOfficial(p.Member))
                : all.Where(p => !MemberLookup.IsOfficial(p.Member));

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
            };

            var recentLogs = await _adminLogService.GetRecentAsync(50);
            vm.RecentLogs = recentLogs.Where(l => l.TargetResource == "VlogPosts").Take(20).ToList();

            ViewData["Title"] = "Vlog 行程文章";
            return View(vm);
        }

        // GET /Admin/VlogPosts/Details/5
        // 唯讀審核頁，給主管／系統管理員用；小編看不到這頁的操作按鈕。
        public async Task<IActionResult> Details(int id)
        {
            var post = await _context.VlogPosts.AsNoTracking().Include(p => p.Member).FirstOrDefaultAsync(p => p.PostId == id);
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
            var reports = _reportService.GetRelatedReports(new LazyTravel.Shared.Models.Report { Id = -1, TargetType = ReportTargetType.VlogPost, TargetId = id });
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
                LastReviewer = reviewLog?.AdminName,
                LastReviewedAt = reviewLog?.CreatedAt,
                LastReviewNote = latestJudged?.AdminNotes,
				Permissions = VlogPostPermissions.For(post, hasPendingReport, User),
			};

            return View(vm);
        }

        // GET /Admin/VlogPosts/Create
        [Authorize(Policy = "RequireVlogCreate")]
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "新增文章";
            // Cookie 認證尚未接上，先固定用 LazyTravel 官方當發文會員
            var officialId = await ResolveOfficialMemberIdAsync();
            var official = await _context.Users.AsNoTracking().FirstOrDefaultAsync(m => m.Id == officialId);
            return View(new VlogPost { TravelDays = 1, MemberId = officialId, Member = official });
        }

        // POST /Admin/VlogPosts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireVlogCreate")]
        public async Task<IActionResult> Create(VlogPost post, IFormFile? coverFile)
        {
            if (coverFile is not null && coverFile.Length > 0 && !IsAllowedImage(coverFile))
            {
                ModelState.AddModelError(string.Empty, "封面只接受 jpg / png / gif / webp 圖片檔，且大小上限 5MB。");
            }

            if (coverFile is null || coverFile.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "請上傳封面圖片。");
            }

            if (string.IsNullOrWhiteSpace(ContentModerationHelper.StripHtml(post.Content)))
            {
                ModelState.AddModelError(nameof(VlogPost.Content), "請輸入行程總體心得。");
            }
            else if (ContentModerationHelper.ContainsProfanity(post.Content))
            {
                ModelState.AddModelError(nameof(VlogPost.Content), "內容包含不當字眼，請修改後再送出。");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "新增文章";
                post.Member = await _context.Users.AsNoTracking().FirstOrDefaultAsync(m => m.Id == post.MemberId);
                return View(post);
            }

            if (coverFile is not null && coverFile.Length > 0)
            {
                post.MediaUrl = await _imageUploadService.UploadAsync(coverFile, "cover");
            }

            post.CreatedAt = DateTime.Now;
            post.UpdatedAt = null;
            post.IsDelete = false;

            _context.VlogPosts.Add(post);
            await _context.SaveChangesAsync();

            await _adminLogService.WriteAsync(
                GetCurrentEmployeeId(),
                "新增文章",
                $"新增文章「{post.Title}」",
                targetResource: "VlogPosts",
                targetId: post.PostId.ToString());

            TempData["SuccessMessage"] = $"文章「{post.Title}」已新增，可以繼續往下新增每日行程。";
            return RedirectToAction(nameof(Edit), new { id = post.PostId });
        }

        // GET /Admin/VlogPosts/Edit/5
        // 小編編輯自己草稿用的頁面。editId 有值時，下方行程表單切成「編輯行程」模式。
        [Authorize(Policy = "RequireVlogUpdate")]
        public async Task<IActionResult> Edit(int id, int? editId = null)
        {
            var post = await _context.VlogPosts.AsNoTracking().Include(p => p.Member).FirstOrDefaultAsync(p => p.PostId == id);
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
        [Authorize(Policy = "RequireVlogUpdate")]
        public async Task<IActionResult> Edit(int id, VlogPost post, IFormFile? coverFile, string? existingMediaUrl, string action = "draft")
        {
            post.PostId = id;

            var existing = await _context.VlogPosts.Include(p => p.Member).FirstOrDefaultAsync(p => p.PostId == id);
            if (existing is null)
            {
                return NotFound();
            }

            // Edit 頁只給小編編輯「官方文章」的草稿：會員文章一律不能編輯，
            // 官方文章已送審/已發布/已刪除的話要先請主管退回草稿才能再編輯。
            // 防止有人繞過畫面上的權限判斷直接送 POST。
            if (!MemberLookup.IsOfficial(existing.Member) || existing.IsDelete || existing.Status != VlogPostStatus.Draft)
            {
                return Forbid();
            }

            if (coverFile is not null && coverFile.Length > 0 && !IsAllowedImage(coverFile))
            {
                ModelState.AddModelError(string.Empty, "封面只接受 jpg / png / gif / webp 圖片檔，且大小上限 5MB。");
            }

            if ((coverFile is null || coverFile.Length == 0) && string.IsNullOrWhiteSpace(existingMediaUrl))
            {
                ModelState.AddModelError(string.Empty, "請上傳封面圖片。");
            }

            if (string.IsNullOrWhiteSpace(ContentModerationHelper.StripHtml(post.Content)))
            {
                ModelState.AddModelError(nameof(VlogPost.Content), "請輸入行程總體心得。");
            }
            else if (ContentModerationHelper.ContainsProfanity(post.Content))
            {
                ModelState.AddModelError(nameof(VlogPost.Content), "內容包含不當字眼，請修改後再送出。");
            }

            if (!ModelState.IsValid)
            {
                post.MediaUrl = existingMediaUrl;
                post.Member = existing.Member;
                ViewData["Title"] = "編輯文章";
                return View(await BuildItineraryViewModelAsync(post, null));
            }

            post.MediaUrl = coverFile is not null && coverFile.Length > 0
                ? await _imageUploadService.UploadAsync(coverFile, "cover")
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
                GetCurrentEmployeeId(),
                isSubmit ? "送出審核" : "儲存草稿",
                isSubmit ? $"文章「{post.Title}」已送出審核" : $"編輯文章「{post.Title}」",
                targetResource: "VlogPosts",
                targetId: post.PostId.ToString());

            TempData["SuccessMessage"] = isSubmit ? $"文章「{post.Title}」已送出審核。" : $"文章「{post.Title}」已儲存為草稿。";
            return RedirectToAction(nameof(Index));
        }

        // POST /Admin/VlogPosts/Delete/5（軟刪除，設定 IsDelete = true）
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireContentDelete")]
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
                GetCurrentEmployeeId(),
                "刪除文章",
                $"刪除文章「{post.Title}」",
                targetResource: "VlogPosts",
                targetId: post.PostId.ToString());

            TempData["SuccessMessage"] = $"文章「{post.Title}」已刪除，可在「已刪除」分頁還原。";
            return RedirectToAction(nameof(Index));
        }

        // POST /Admin/VlogPosts/Restore/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireVlogRestore")]
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
                GetCurrentEmployeeId(),
                "還原文章",
                $"還原文章「{post.Title}」",
                targetResource: "VlogPosts",
                targetId: post.PostId.ToString());

            TempData["SuccessMessage"] = $"文章「{post.Title}」已還原。";
            return RedirectToAction(nameof(Index));
        }

        // POST /Admin/VlogPosts/ToggleStatus/5（草稿 <-> 已發布 快速切換；主管在檢視頁按「審核通過」「退回草稿」也是走這裡）
        // 退回草稿一定要附備註：小編才知道要改哪裡，備註存進 AdminLogs 的 Detail 欄位，不用另外開欄位/資料表，
        // Edit 頁再把最近一次的「退回草稿」紀錄撈出來顯示給小編看。
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ToggleStatus(int id, VlogPostStatus to, string? note = null, bool fromDetails = false)
        {
            var post = await _context.VlogPosts.Include(p => p.Member).FirstOrDefaultAsync(p => p.PostId == id);
            if (post is null)
            {
                return NotFound();
            }

            // 草稿/送審/已發布這條流程只適用官方文章：會員文章不是小編寫的，沒有送審/退回草稿這件事。
            if (post.IsDelete || !MemberLookup.IsOfficial(post.Member))
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

            // ToggleStatus 一支 action 同時處理三種轉換,各自需要的權限不一樣(比照 VlogPostPermissions.For 的判斷),
            // [Authorize] 屬性只能擋「沒登入」,實際「有沒有做這件事的權限」要照轉換目標分開檢查。
            var hasSuperAdmin = User.HasClaim("Permission", "ROLE_SUPER_ADMIN");
            var requiredPermission = to switch
            {
                VlogPostStatus.PendingReview => "content:vlog:submit",
                VlogPostStatus.Published => "content:vlog:publish",
                VlogPostStatus.Draft => "content:vlog:return",
                _ => null,
            };
            if (requiredPermission is not null && !hasSuperAdmin && !User.HasClaim("Permission", requiredPermission))
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
                GetCurrentEmployeeId(),
                action,
                detail,
                targetResource: "VlogPosts",
                targetId: post.PostId.ToString());

            TempData["SuccessMessage"] = $"文章「{post.Title}」已更新為「{to.ToLabel()}」。";
            return fromDetails ? RedirectToAction(nameof(Details), new { id }) : RedirectToAction(nameof(Index));
        }

        // POST /Admin/VlogPosts/ReportPost/5（小編對會員文章提出檢舉；官方文章不走這裡，是自己人不用檢舉自己）
        [HttpPost]
        [ValidateAntiForgeryToken]
		[Authorize(Policy = "RequireVlogAudit")]
		public async Task<IActionResult> ReportPost(int id, ReportReasonCategory reasonCategory, string reason)
        {
            var post = await _context.VlogPosts.Include(p => p.Member).FirstOrDefaultAsync(p => p.PostId == id);
            if (post is null)
            {
                return NotFound();
            }

            if (MemberLookup.IsOfficial(post.Member))
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
                post.Member?.Name ?? $"會員 #{post.MemberId}",
                User.Identity?.Name ?? "小編",
                reasonCategory,
                reason,
                GetCurrentEmployeeId());

            // 會員文章被檢舉後，Status 跟著改成「待審核」，讓列表頁可以直接靠 Status 判斷要顯示
            // 「查看檢舉」還是「提出檢舉」，不用另外查 Reports 表。判定不成立後會改回「已發布」。
            post.Status = VlogPostStatus.PendingReview;
            post.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"已對文章「{post.Title}」提出檢舉，主管會盡快處理。";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST /Admin/VlogPosts/DismissReport/5（主管判定會員文章的檢舉不成立；官方文章走「退回草稿」，不走這裡）
        [HttpPost]
        [ValidateAntiForgeryToken]
		[Authorize(Policy = "RequireReportAudit")]
		public async Task<IActionResult> DismissReport(int id)
        {
            var post = await _context.VlogPosts.Include(p => p.Member).FirstOrDefaultAsync(p => p.PostId == id);
            if (post is null)
            {
                return NotFound();
            }

            if (MemberLookup.IsOfficial(post.Member))
            {
                return Forbid();
            }

            var reports = _reportService.GetRelatedReports(new LazyTravel.Shared.Models.Report { Id = -1, TargetType = ReportTargetType.VlogPost, TargetId = id });
            var pendingReport = reports.FirstOrDefault(r => r.Status == ReportStatus.Pending);
            if (pendingReport is null)
            {
                TempData["ErrorMessage"] = "目前沒有待處理的檢舉。";
                return RedirectToAction(nameof(Details), new { id });
            }

            await _reportService.JudgeAsync(pendingReport.Id, ReportStatus.Dismissed, "於 Vlog 行程文章後台判定不成立", isMalicious: false, GetCurrentEmployeeId());

            // 檢舉不成立，文章 Status 改回「已發布」，跟「提出檢舉時改成待審核」互相對應。
            post.Status = VlogPostStatus.Published;
            post.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"文章「{post.Title}」的檢舉已判定為不成立。";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ---------- 每日行程列表管理（併入「編輯文章」頁面下方） ----------

        // 組出「編輯文章」頁面用的 ViewModel：文章本身 + 當天行程清單 + 目前正在編輯的行程節點
        private async Task<ItineraryViewModel> BuildItineraryViewModelAsync(VlogPost post, int? editId)
        {
            var nodes = await _context.ItineraryNodes.AsNoTracking()
                .Where(n => n.PostId == post.PostId)
                .OrderBy(n => n.DayNumber).ThenBy(n => n.ArrivalTime).ToListAsync();

            var editingNode = editId.HasValue ? nodes.FirstOrDefault(n => n.NodeId == editId.Value) : null;
            var (editingHours, editingMinutes) = SplitStayMinutes(editingNode?.StayTime);

            var logs = await _adminLogService.GetForTargetAsync("VlogPosts", post.PostId.ToString());
            var lastReturn = logs.Where(l => l.Action == "退回草稿").OrderByDescending(l => l.CreatedAt).FirstOrDefault();

            return new ItineraryViewModel
            {
                Post = post,
                Nodes = nodes,
                EditingNode = editingNode,
                EditingStayHours = editingHours,
                EditingStayMinutes = editingMinutes,
				Permissions = VlogPostPermissions.For(post, hasPendingReport: false, User),
				LastReturnNote = lastReturn?.Description,
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
            var parentPost = await _context.VlogPosts.AsNoTracking().Include(p => p.Member).FirstOrDefaultAsync(p => p.PostId == postId);
            if (parentPost is null)
            {
                return NotFound();
            }

            if (!MemberLookup.IsOfficial(parentPost.Member) || parentPost.IsDelete || parentPost.Status != VlogPostStatus.Draft)
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
                mediaUrl = await _imageUploadService.UploadAsync(mediaFile, "itinerary");
            }

            _context.ItineraryNodes.Add(new ItineraryNode
            {
                PostId = postId,
                DayNumber = dayNumber < 1 ? 1 : dayNumber,
                LocationName = locationName,
                ArrivalTime = TimeOnly.TryParse(arrivalTime, out var at) ? at : null,
                StayTime = ParseStayDurationToMinutes(stayDuration),
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

        // 「停留時間」表單欄位是 <input type="time">，借用時鐘格式(HH:mm)表示「持續多久」而不是「幾點幾分」。
        // 例如輸入 02:35 代表停留 2 小時 35 分鐘，跟抵達/離開時間欄位是同一種輸入元件，操作起來一致。
        // 🌟 StayTime 這次資料庫重建後改存分鐘數（int?），這裡直接算成總分鐘數，不用再組文字。
        private static int? ParseStayDurationToMinutes(string? stayDuration)
        {
            if (string.IsNullOrWhiteSpace(stayDuration))
            {
                return null;
            }

            var parts = stayDuration.Split(':');
            var hours = parts.Length > 0 && int.TryParse(parts[0], out var h) ? h : 0;
            var minutes = parts.Length > 1 && int.TryParse(parts[1], out var m) ? m : 0;

            var totalMinutes = hours * 60 + minutes;
            return totalMinutes > 0 ? totalMinutes : null;
        }

        // ParseStayDurationToMinutes 的反向操作：把分鐘數拆回小時/分鐘，供編輯表單預填時間欄位。
        private static (int Hours, int Minutes) SplitStayMinutes(int? totalMinutes)
        {
            if (!totalMinutes.HasValue || totalMinutes.Value <= 0)
            {
                return (0, 0);
            }

            return (totalMinutes.Value / 60, totalMinutes.Value % 60);
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

            var parentPost = await _context.VlogPosts.AsNoTracking().Include(p => p.Member).FirstOrDefaultAsync(p => p.PostId == postId);
            if (parentPost is null)
            {
                return NotFound();
            }

            if (!MemberLookup.IsOfficial(parentPost.Member) || parentPost.IsDelete || parentPost.Status != VlogPostStatus.Draft)
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
                ? await _imageUploadService.UploadAsync(mediaFile, "itinerary")
                : existingMediaUrl;

            node.DayNumber = dayNumber < 1 ? 1 : dayNumber;
            node.LocationName = locationName;
            node.ArrivalTime = TimeOnly.TryParse(arrivalTime, out var at) ? at : null;
            node.StayTime = ParseStayDurationToMinutes(stayDuration);
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
            var parentPost = await _context.VlogPosts.AsNoTracking().Include(p => p.Member).FirstOrDefaultAsync(p => p.PostId == postId);
            if (parentPost is null)
            {
                return NotFound();
            }

            if (!MemberLookup.IsOfficial(parentPost.Member) || parentPost.IsDelete || parentPost.Status != VlogPostStatus.Draft)
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

        // Cookie 認證尚未接上，新增文章時先固定用「LazyTravel 官方」這筆 Member 當發文會員。
        // 用 Email(MemberLookup.OfficialAccountEmail) 查真的 MemberID，不寫死數字，換資料庫也不會查錯。
        private async Task<int> ResolveOfficialMemberIdAsync()
        {
            if (_cachedOfficialMemberId.HasValue)
            {
                return _cachedOfficialMemberId.Value;
            }

            var official = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Email == MemberLookup.OfficialAccountEmail);
            if (official is null)
            {
                throw new InvalidOperationException(
                    $"找不到官方帳號（Email = {MemberLookup.OfficialAccountEmail}），請先在 Members 資料表建立這筆資料。");
            }

            _cachedOfficialMemberId = official.Id;
            return official.Id;
        }

        // ---------- 檔案上傳共用小工具 ----------

        private static bool IsAllowedImage(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            return file.Length <= MaxUploadSizeBytes && AllowedImageExtensions.Contains(ext);
        }

    }
}
