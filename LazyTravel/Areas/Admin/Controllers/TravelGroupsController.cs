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

        // 審核狀態篩選僅開放這 3 種（配合前台簡化後的下拉選單）
        private static readonly string[] AllowedReviewStatuses =
        {
            "正常", "檢舉審核中", "違規"
        };

        private static readonly string[] AbnormalReviewStatuses =
        {
            "檢舉審核中", "違規"
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
            int abnormalPage = 1,
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
            var abnormalQuery = query.Where(g => g.IsDelete == false && AbnormalReviewStatuses.Contains(g.ReviewStatus));
            var deletedQuery = query.Where(g => g.IsDelete == true);

            // 排序：預設編號小到大
            if (sortOrder == "desc")
            {
                activeQuery = activeQuery.OrderByDescending(g => g.GroupId);
                abnormalQuery = abnormalQuery.OrderByDescending(g => g.GroupId);
                deletedQuery = deletedQuery.OrderByDescending(g => g.GroupId);
            }
            else
            {
                activeQuery = activeQuery.OrderBy(g => g.GroupId);
                abnormalQuery = abnormalQuery.OrderBy(g => g.GroupId);
                deletedQuery = deletedQuery.OrderBy(g => g.GroupId);
            }

            if (activePage < 1)
            {
                activePage = 1;
            }

            if (abnormalPage < 1)
            {
                abnormalPage = 1;
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
            var abnormalTotalCount = await abnormalQuery.CountAsync();
            var deletedTotalCount = await deletedQuery.CountAsync();

            // 異動紀錄清單：依建立時間新到舊排序，不套用揪團篩選條件（獨立瀏覽異動歷程）
            var logQuery = _context.TravelGroupsLogs
                .Include(l => l.Group)
                .Include(l => l.ChangedByMember)
                .OrderByDescending(l => l.CreatedAt)
                .AsQueryable();

            var logTotalCount = await logQuery.CountAsync();

            var vm = new TravelGroupAdminIndexViewModel
            {
                ActiveGroups = await activeQuery
                    .Skip((activePage - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(),

                AbnormalGroups = await abnormalQuery
                    .Skip((abnormalPage - 1) * pageSize)
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
                AbnormalPage = abnormalPage,
                DeletedPage = deletedPage,
                LogPage = logPage,

                ActiveTotalPages = (int)Math.Ceiling(activeTotalCount / (double)pageSize),
                AbnormalTotalPages = (int)Math.Ceiling(abnormalTotalCount / (double)pageSize),
                DeletedTotalPages = (int)Math.Ceiling(deletedTotalCount / (double)pageSize),
                LogTotalPages = (int)Math.Ceiling(logTotalCount / (double)pageSize),

                PageSize = pageSize,
                Tab = tab,

                TotalGroupsCount = activeTotalCount,
                AbnormalGroupsCount = abnormalTotalCount,
                DeletedGroupsCount = deletedTotalCount,
                LogsCount = logTotalCount
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
            var group = await FindGroupDetailsAsync(id);

            if (group == null)
            {
                return NotFound();
            }

            return View(group);
        }

        public async Task<IActionResult> DetailsPanel(int id)
        {
            var group = await FindGroupDetailsAsync(id);

            if (group == null)
            {
                return NotFound();
            }

            return PartialView("_TravelGroupDetailsContent", group);
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

            // 只有已判定違規的揪團才允許後台軟刪除
            if (group.ReviewStatus != "違規")
            {
                TempData["ErrorMessage"] = "只有已判定違規的揪團可以刪除。";
                return RedirectToAction(nameof(Index), new { tab = "active" });
            }

            group.IsDelete = true;
            group.IsPublic = false;
            group.UpdatedAt = DateTime.Now;

            _context.TravelGroupsLogs.Add(new TravelGroupsLog
            {
                GroupId = group.GroupId,
                // TODO(後續)：串接登入驗證後，改寫入實際操作的管理員 MemberID。
                // 目前資料庫欄位不可為 NULL，暫以團主 ID 作為系統操作紀錄的占位值。
                ChangedByMemberId = group.OwnerMemberId,
                ChangeType = "刪除",
                FieldName = "IsDelete",
                OldValue = "0",
                NewValue = "1",
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

            var oldReviewStatus = group.ReviewStatus;

            // 還原代表從已刪除清單移回所有揪團清單，審核狀態回復正常
            group.IsDelete = false;
            group.ReviewStatus = "正常";
            group.IsPublic = true;
            group.UpdatedAt = DateTime.Now;

            _context.TravelGroupsLogs.Add(new TravelGroupsLog
            {
                GroupId = group.GroupId,
                // TODO(後續)：串接登入驗證後，改寫入實際操作的管理員 MemberID。
                // 目前資料庫欄位不可為 NULL，暫以團主 ID 作為系統操作紀錄的占位值。
                ChangedByMemberId = group.OwnerMemberId,
                ChangeType = "還原",
                FieldName = "ReviewStatus",
                OldValue = oldReviewStatus,
                NewValue = "正常",
                Remark = "由已刪除清單還原至所有揪團清單，審核狀態回復正常。",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "揪團已還原至正常清單。";
            return RedirectToAction(nameof(Index), new { tab = "active" });
        }

        private Task<TravelGroup?> FindGroupDetailsAsync(int id)
        {
            return _context.TravelGroups
                .Include(g => g.OwnerMember)
                .Include(g => g.GroupMembers)
                    .ThenInclude(gm => gm.Member)
                .FirstOrDefaultAsync(g => g.GroupId == id);
        }
    }
}
