using LazyTravel.Models;
using LazyTravel.Models.EfModels;
using LazyTravel.Services;
using LazyTravel.ViewModels;
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
        public async Task<IActionResult> Create(ReportSubmitViewModel form)
        {
            var reporter = await _context.Members.FirstOrDefaultAsync(m => m.Email == form.ReporterEmail);
            if (reporter == null)
            {
                ModelState.AddModelError(nameof(form.ReporterEmail), "查無此帳號,請確認 Email 是否正確");
            }

            var reportedMember = await _context.Members.FirstOrDefaultAsync(m => m.Email == form.ReportedMemberEmail);
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
            var report = new LazyTravel.Models.EfModels.Report
            {
                ReporterId = reporter!.MemberId,
                ReportedMemberId = reportedMember!.MemberId,
                ReportType = (byte)form.TargetType,
                // 檢舉「會員」類型時,被檢舉內容就是這個會員本人,對象編號直接沿用會員編號
                TargetId = form.TargetType == ReportTargetType.Member ? reportedMember.MemberId : form.TargetId,
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
    }
}
