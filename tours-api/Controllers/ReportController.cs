using LazyTravel.Shared.Models;
using LazyTravel.Shared.Models.EfModels;
using LazyTravel.Shared.Services;
using LazyTravel.Shared.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Controllers
{
    // 前台(會員端)送出檢舉的表單。目前還沒有登入系統跟 Vlog/會員/揪團瀏覽頁,
    // 所以檢舉人、被檢舉對象都先用 Email/編號手動輸入,之後這些頁面做出來可以改成自動帶入現有登入者、
    // 從內容頁直接帶 TargetId 過來。
    public class ReportController : Controller
    {
        private readonly LazyTravelDBContext _context;
        private readonly IImageStorageService _imageStorage;

        public ReportController(LazyTravelDBContext context, IImageStorageService imageStorage)
        {
            _context = context;
            _imageStorage = imageStorage;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new ReportSubmitViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitTargetReport([FromForm] TargetReportRequest form)
        {
            if (!Enum.TryParse<ReportTargetType>(form.TargetType, ignoreCase: true, out var targetType) ||
                targetType is not (ReportTargetType.TravelGroup or ReportTargetType.VlogPost))
            {
                return BadRequest(new { message = "檢舉種類不正確。" });
            }

            if (form.TargetId <= 0)
            {
                return BadRequest(new { message = "找不到要檢舉的對象。" });
            }

            if (!Enum.IsDefined(typeof(ReportReasonCategory), form.ReasonCategory))
            {
                return BadRequest(new { message = "請選擇檢舉原因。" });
            }

            if (form.Evidence is null || form.Evidence.Length == 0)
            {
                return BadRequest(new { message = "請上傳檢舉截圖。" });
            }

            if (form.Evidence.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { message = "截圖檔案不可超過 5MB。" });
            }

            var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };
            if (string.IsNullOrWhiteSpace(form.Evidence.ContentType) || !allowedTypes.Contains(form.Evidence.ContentType))
            {
                return BadRequest(new { message = "截圖僅支援 JPG、PNG、WEBP。" });
            }

            var target = await ResolveReportTargetAsync(targetType, form.TargetId);
            if (target is null)
            {
                return NotFound(new { message = "找不到要檢舉的內容。" });
            }

            var reporterId = await GetActingMemberIdAsync(form.ViewerMemberId);
            if (target.ReportedMemberId == reporterId)
            {
                return BadRequest(new { message = "不能檢舉自己建立的內容。" });
            }

            var evidenceUrl = await _imageStorage.UploadAsync(form.Evidence, "reports");
            var category = (ReportReasonCategory)form.ReasonCategory;

            var report = new LazyTravel.Shared.Models.EfModels.Report
            {
                ReporterId = reporterId,
                ReportedMemberId = target.ReportedMemberId,
                ReportType = (byte)targetType,
                TargetId = target.TargetId,
                TargetTitle = target.TargetTitle,
                ReasonCategory = (byte)category,
                Reason = category.ToDisplayName(),
                Description = SafeText(form.Description, 500),
                EvidenceUrl = evidenceUrl,
                ReportStatus = (byte)ReportStatus.Pending,
                CreatedAt = DateTime.Now,
                IsMalicious = false,
                TargetSnapshot = target.Snapshot
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return Json(new { ok = true, message = "檢舉已送出，後台審核人員會盡快處理。" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReportSubmitViewModel form)
        {
            var reporter = await _context.Users.FirstOrDefaultAsync(m => m.Email == form.ReporterEmail);
            if (reporter == null)
            {
                ModelState.AddModelError(nameof(form.ReporterEmail), "查無此帳號,請確認 Email 是否正確");
            }

            var reportedMember = await _context.Users.FirstOrDefaultAsync(m => m.Email == form.ReportedMemberEmail);
            if (reportedMember == null)
            {
                ModelState.AddModelError(nameof(form.ReportedMemberEmail), "查無此帳號,請確認 Email 是否正確");
            }

            if (form.Evidence != null && form.Evidence.Length == 0)
            {
                ModelState.AddModelError(nameof(form.Evidence), "請上傳檢舉截圖");
            }

            if (!ModelState.IsValid)
            {
                return View(form);
            }

            var evidenceUrl = await _imageStorage.UploadAsync(form.Evidence!, "檢舉");

            // EfModels.Report 欄位是 byte/raw 型別(對齊資料表),enum 要轉型別再存
            var report = new LazyTravel.Shared.Models.EfModels.Report
            {
                ReporterId = reporter!.Id,
                ReportedMemberId = reportedMember!.Id,
                ReportType = (byte)form.TargetType,
                // 檢舉「會員」類型時,被檢舉內容就是這個會員本人,對象編號直接沿用會員編號
                TargetId = form.TargetType == ReportTargetType.Member ? reportedMember.Id : form.TargetId,
                TargetTitle = form.TargetType == ReportTargetType.Member ? reportedMember.Name : form.TargetTitle,
                ReasonCategory = (byte)form.ReasonCategory,
                Reason = form.Reason,
                Description = form.Description,
                EvidenceUrl = evidenceUrl,
                ReportStatus = (byte)ReportStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "檢舉已送出,後台審核人員會盡快處理。";
            return RedirectToAction(nameof(Create));
        }

        private async Task<ReportTargetInfo?> ResolveReportTargetAsync(ReportTargetType targetType, int targetId)
        {
            if (targetType == ReportTargetType.TravelGroup)
            {
                var group = await _context.TravelGroups.AsNoTracking()
                    .FirstOrDefaultAsync(g => g.GroupId == targetId && !g.IsDelete);
                return group is null
                    ? null
                    : new ReportTargetInfo(group.GroupId, group.OwnerMemberId, group.GroupTitle, $"揪團｜{group.Country}｜{group.Region}｜{group.StartDate:yyyy/MM/dd}-{group.EndDate:yyyy/MM/dd}");
            }

            if (targetType == ReportTargetType.VlogPost)
            {
                var post = await _context.VlogPosts.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PostId == targetId && !p.IsDelete);
                return post is null
                    ? null
                    : new ReportTargetInfo(post.PostId, post.MemberId, post.Title, $"文章｜{post.Destination}｜{post.TravelDate:yyyy/MM/dd}｜{post.Status}");
            }

            return null;
        }

        private async Task<int> GetActingMemberIdAsync(int? viewerMemberId)
        {
            if (viewerMemberId.HasValue && await _context.Users.AnyAsync(m => m.Id == viewerMemberId.Value))
            {
                return viewerMemberId.Value;
            }
            return await GetOrCreateVisitorMemberIdAsync();
        }

        private const string VisitorMemberCookieName = "ltvmid";

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
                .Skip(1)
                .FirstOrDefaultAsync();

            if (candidateId == 0)
            {
                candidateId = await _context.Users.AsNoTracking().OrderBy(m => m.Id).Select(m => m.Id).FirstAsync();
            }

            Response.Cookies.Append(VisitorMemberCookieName, candidateId.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(2),
                IsEssential = true,
                HttpOnly = true,
            });
            return candidateId;
        }

        private static string? SafeText(string? value, int maxLength)
        {
            var text = value?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return null;
            return text.Length <= maxLength ? text : text[..maxLength];
        }

        public sealed class TargetReportRequest
        {
            public string? TargetType { get; set; }
            public int TargetId { get; set; }
            public int? ViewerMemberId { get; set; }
            public byte ReasonCategory { get; set; }
            public string? Description { get; set; }
            public IFormFile? Evidence { get; set; }
        }

        private sealed record ReportTargetInfo(int TargetId, int ReportedMemberId, string? TargetTitle, string? Snapshot);
    }
}
