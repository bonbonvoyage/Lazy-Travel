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

        public TravelGroupsController(LazyTravelContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? keyword,
            DateTime? createdAtFrom,
            DateTime? createdAtTo,
            List<string>? selectedJoinRules,
            List<string>? selectedGroupStatuses,
            List<string>? selectedReviewStatuses,
            List<string>? selectedPublicStatuses,
            string sortOrder = "asc",
            int activePage = 1,
            int deletedPage = 1,
            string tab = "active")
        {
            int pageSize = 10;

            selectedJoinRules ??= new List<string>();
            selectedGroupStatuses ??= new List<string>();
            selectedReviewStatuses ??= new List<string>();
            selectedPublicStatuses ??= new List<string>();

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

            // 加入規則複選
            if (selectedJoinRules.Any())
            {
                query = query.Where(g =>
                    g.JoinRule != null &&
                    selectedJoinRules.Contains(g.JoinRule));
            }

            // 揪團狀態複選
            if (selectedGroupStatuses.Any())
            {
                query = query.Where(g =>
                    g.GroupStatus != null &&
                    selectedGroupStatuses.Contains(g.GroupStatus));
            }

            // 審核狀態複選
            if (selectedReviewStatuses.Any())
            {
                query = query.Where(g =>
                    g.ReviewStatus != null &&
                    selectedReviewStatuses.Contains(g.ReviewStatus));
            }

            // 公開狀態複選
            if (selectedPublicStatuses.Any())
            {
                bool hasPublic = selectedPublicStatuses.Contains("公開");
                bool hasPrivate = selectedPublicStatuses.Contains("不公開");

                if (hasPublic && !hasPrivate)
                {
                    query = query.Where(g => g.IsPublic == true);
                }
                else if (!hasPublic && hasPrivate)
                {
                    query = query.Where(g => g.IsPublic == false);
                }
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

            var activeTotalCount = await activeQuery.CountAsync();
            var deletedTotalCount = await deletedQuery.CountAsync();

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

                Keyword = keyword,
                CreatedAtFrom = createdAtFrom,
                CreatedAtTo = createdAtTo,

                SelectedJoinRules = selectedJoinRules,
                SelectedGroupStatuses = selectedGroupStatuses,
                SelectedReviewStatuses = selectedReviewStatuses,
                SelectedPublicStatuses = selectedPublicStatuses,

                SortOrder = sortOrder,

                ActivePage = activePage,
                DeletedPage = deletedPage,
                ActiveTotalPages = (int)Math.Ceiling(activeTotalCount / (double)pageSize),
                DeletedTotalPages = (int)Math.Ceiling(deletedTotalCount / (double)pageSize),
                PageSize = pageSize,
                Tab = tab
            };

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

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "揪團已還原至正常清單。";
            return RedirectToAction(nameof(Index), new { tab = "active" });
        }
    }
}