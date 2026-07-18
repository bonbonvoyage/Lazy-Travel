using LazyTravel.Models;
using LazyTravel.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TravelGroupsController : Controller
    {
        private readonly LazyTravelContext _context;

        // 審核狀態篩選僅開放這 4 種（配合前台簡化後的下拉選單）
        private static readonly string[] AllowedReviewStatuses =
        {
            "正常", "檢舉審核中", "違規", "已下架"
        };

        public TravelGroupsController(LazyTravelContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? keyword,
            DateTime? createdAtFrom,
            DateTime? createdAtTo,
            string? selectedReviewStatus,
            string sortOrder = "asc",
            int activePage = 1,
            int deletedPage = 1,
            int logPage = 1,
            string tab = "active")
        {
            int pageSize = 10;

            if (!string.IsNullOrWhiteSpace(selectedReviewStatus) &&
                !AllowedReviewStatuses.Contains(selectedReviewStatus))
            {
                selectedReviewStatus = null;
            }

            var query = _context.TravelGroups
                .Include(g => g.OwnerMember)
                .AsQueryable();

            // 關鍵字搜尋：編號、房間名稱、說明、國家、地區、團主
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                bool isNumber = int.TryParse(keyword, out int groupId);

                query = query.Where(g =>
                    (isNumber && g.GroupId == groupId) ||
                    g.GroupTitle.Contains(keyword) ||
                    (g.Description != null && g.Description.Contains(keyword)) ||
                    (g.Country != null && g.Country.Contains(keyword)) ||
                    (g.Region != null && g.Region.Contains(keyword)) ||
                    (g.OwnerMember != null && g.OwnerMember.Name.Contains(keyword)));
            }

            // 建立時間起
            if (createdAtFrom.HasValue)
            {
                query = query.Where(g => g.CreatedAt >= createdAtFrom.Value);
            }

            // 建立時間迄，包含當天整日
            if (createdAtTo.HasValue)
            {
                var endDate = createdAtTo.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(g => g.CreatedAt <= endDate);
            }

            // 審核狀態單選（唯一保留的篩選分類）
            if (!string.IsNullOrWhiteSpace(selectedReviewStatus))
            {
                query = query.Where(g => g.ReviewStatus == selectedReviewStatus);
            }

            var activeQuery = query.Where(g => g.IsDelete == false);
            var deletedQuery = query.Where(g => g.IsDelete == true);

            // 排序：預設編號小到大
            if (sortOrder == "desc")
            {
                activeQuery = activeQuery.OrderByDescending(g => g.GroupId);
                deletedQuery = deletedQuery.OrderByDescending(g => g.GroupId);
            }
            else
            {
                activeQuery = activeQuery.OrderBy(g => g.GroupId);
                deletedQuery = deletedQuery.OrderBy(g => g.GroupId);
            }

            if (activePage < 1)
            {
                activePage = 1;
            }

            if (deletedPage < 1)
            {
                deletedPage = 1;
            }

            if (logPage < 1)
            {
                logPage = 1;
            }

            var activeTotalCount = await activeQuery.CountAsync();
            var deletedTotalCount = await deletedQuery.CountAsync();

            // 異動紀錄清單：依建立時間新到舊排序，不套用揪團篩選條件（獨立瀏覽異動歷程）
            var logQuery = _context.TravelGroupsLogs
                .Include(l => l.Group)
                .Include(l => l.ChangedByMember)
                .OrderByDescending(l => l.CreatedAt)
                .AsQueryable();

            var logTotalCount = await logQuery.CountAsync();

            // ---- 儀表板統計（不受篩選條件影響，反映全站現況） ----
            var totalGroupsCount = await _context.TravelGroups
                .Where(g => g.IsDelete == false)
                .CountAsync();

            var abnormalGroupsCount = await _context.TravelGroups
                .Where(g => g.IsDelete == false && g.ReviewStatus != "正常")
                .CountAsync();

            var now = DateTime.Now;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);

            var thisMonthNewCount = await _context.TravelGroups
                .Where(g => g.CreatedAt >= thisMonthStart && g.CreatedAt < thisMonthStart.AddMonths(1))
                .CountAsync();

            var lastMonthNewCount = await _context.TravelGroups
                .Where(g => g.CreatedAt >= lastMonthStart && g.CreatedAt < thisMonthStart)
                .CountAsync();

            double? growthRatePercent;
            if (lastMonthNewCount == 0)
            {
                growthRatePercent = thisMonthNewCount > 0 ? (double?)null : 0;
            }
            else
            {
                growthRatePercent = Math.Round(
                    (thisMonthNewCount - lastMonthNewCount) / (double)lastMonthNewCount * 100, 1);
            }

            var vm = new TravelGroupAdminIndexViewModel
            {
                ActiveGroups = await activeQuery
                    .Skip((activePage - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(),

                DeletedGroups = await deletedQuery
                    .Skip((deletedPage - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(),

                Logs = await logQuery
                    .Skip((logPage - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(),

                Keyword = keyword,
                CreatedAtFrom = createdAtFrom,
                CreatedAtTo = createdAtTo,

                SelectedReviewStatus = selectedReviewStatus,

                SortOrder = sortOrder,

                ActivePage = activePage,
                DeletedPage = deletedPage,
                LogPage = logPage,

                ActiveTotalPages = (int)Math.Ceiling(activeTotalCount / (double)pageSize),
                DeletedTotalPages = (int)Math.Ceiling(deletedTotalCount / (double)pageSize),
                LogTotalPages = (int)Math.Ceiling(logTotalCount / (double)pageSize),

                PageSize = pageSize,
                Tab = tab,

                TotalGroupsCount = totalGroupsCount,
                AbnormalGroupsCount = abnormalGroupsCount,
                NewGroupsGrowthRatePercent = growthRatePercent
            };

            // AJAX 局部更新：只回傳頁籤與清單區塊，不重整整頁
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_TravelGroupsTabContent", vm);
            }

            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var group = await _context.TravelGroups
                .Include(g => g.OwnerMember)
                .Include(g => g.GroupMembers)
                    .ThenInclude(gm => gm.Member)
                .FirstOrDefaultAsync(g => g.GroupId == id);

            if (group == null)
            {
                return NotFound();
            }

            return View(group);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var group = await _context.TravelGroups
                .FirstOrDefaultAsync(g => g.GroupId == id);

            if (group == null)
            {
                return NotFound();
            }

            // 只有異常審核狀態才允許後台軟刪除
            if (group.ReviewStatus != "檢舉審核中" &&
                group.ReviewStatus != "已下架" &&
                group.ReviewStatus != "違規")
            {
                TempData["ErrorMessage"] = "只有檢舉或異常狀態的揪團可以刪除。";
                return RedirectToAction(nameof(Index), new { tab = "active" });
            }

            group.IsDelete = true;
            group.IsPublic = false;
            group.UpdatedAt = DateTime.Now;

            _context.TravelGroupsLogs.Add(new TravelGroupsLog
            {
                GroupId = group.GroupId,
                // TODO(後續)：串接登入驗證後，改寫入實際操作的管理員 MemberID
                ChangedByMemberId = null,
                ChangeType = "刪除",
                FieldName = null,
                OldValue = null,
                NewValue = null,
                Remark = $"審核狀態為「{group.ReviewStatus}」，移至已刪除清單。",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "揪團已移至已刪除清單。";
            return RedirectToAction(nameof(Index), new { tab = "deleted" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var group = await _context.TravelGroups
                .FirstOrDefaultAsync(g => g.GroupId == id);

            if (group == null)
            {
                return NotFound();
            }

            // 還原只代表從已刪除清單移回正常清單
            group.IsDelete = false;

            // 不主動把 ReviewStatus 改成正常，避免覆蓋原本的檢舉狀態
            if (group.ReviewStatus == "正常" || group.ReviewStatus == "無違規")
            {
                group.IsPublic = true;
            }
            else
            {
                group.IsPublic = false;
            }

            group.UpdatedAt = DateTime.Now;

            _context.TravelGroupsLogs.Add(new TravelGroupsLog
            {
                GroupId = group.GroupId,
                // TODO(後續)：串接登入驗證後，改寫入實際操作的管理員 MemberID
                ChangedByMemberId = null,
                ChangeType = "還原",
                FieldName = null,
                OldValue = null,
                NewValue = null,
                Remark = "由已刪除清單還原至正常清單。",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "揪團已還原至正常清單。";
            return RedirectToAction(nameof(Index), new { tab = "active" });
        }
    }
}
