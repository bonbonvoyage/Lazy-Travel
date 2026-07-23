using LazyTravel.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LazyTravel.Models.ViewModels;

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
    string? country,
    string? region,
    string? ownerName,
    byte? joinRule,
    byte? groupStatus,
    string? reviewStatus,
    bool? isPublic,
    DateTime? startDateFrom,
    DateTime? startDateTo,
    DateTime? createdAtFrom,
    DateTime? createdAtTo,
    int activePage = 1,
    int deletedPage = 1,
    string tab = "active")
        {
            int pageSize = 10;

            var query = _context.TravelGroups
                .Include(g => g.OwnerMember)
                .AsQueryable();

            // 關鍵字：房間名稱 / 說明
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(g =>
                    g.GroupTitle.Contains(keyword) ||
                    (g.Description != null && g.Description.Contains(keyword)));
            }

            // 國家
            if (!string.IsNullOrWhiteSpace(country))
            {
                query = query.Where(g => g.Country != null && g.Country.Contains(country));
            }

            // 地區
            if (!string.IsNullOrWhiteSpace(region))
            {
                query = query.Where(g => g.Region != null && g.Region.Contains(region));
            }

            // 團主
            if (!string.IsNullOrWhiteSpace(ownerName))
            {
                query = query.Where(g => g.OwnerMember != null && g.OwnerMember.Name.Contains(ownerName));
            }

            // 加入規則
            if (joinRule.HasValue)
            {
                query = query.Where(g => g.JoinRule == joinRule.Value);
            }

            // 揪團狀態
            if (groupStatus.HasValue)
            {
                query = query.Where(g => g.GroupStatus == groupStatus.Value);
            }

            // 審核狀態
            if (!string.IsNullOrWhiteSpace(reviewStatus))
            {
                query = query.Where(g => g.ReviewStatus == reviewStatus);
            }

            // 是否公開
            if (isPublic.HasValue)
            {
                query = query.Where(g => g.IsPublic == isPublic.Value);
            }

            // 行程開始時間區間
            if (startDateFrom.HasValue)
            {
                query = query.Where(g => g.StartDate >= startDateFrom.Value);
            }

            if (startDateTo.HasValue)
            {
                query = query.Where(g => g.StartDate <= startDateTo.Value);
            }

            // 建立時間區間
            if (createdAtFrom.HasValue)
            {
                query = query.Where(g => g.CreatedAt >= createdAtFrom.Value);
            }

            if (createdAtTo.HasValue)
            {
                query = query.Where(g => g.CreatedAt <= createdAtTo.Value);
            }

            var activeQuery = query
                .Where(g => g.IsDelete == false)
                .OrderByDescending(g => g.CreatedAt);

            var deletedQuery = query
                .Where(g => g.IsDelete == true)
                .OrderByDescending(g => g.UpdatedAt);

            var activeTotalCount = await activeQuery.CountAsync();
            var deletedTotalCount = await deletedQuery.CountAsync();

            if (activePage < 1) activePage = 1;
            if (deletedPage < 1) deletedPage = 1;

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
                Country = country,
                Region = region,
                OwnerName = ownerName,
                JoinRule = joinRule,
                GroupStatus = groupStatus,
                ReviewStatus = reviewStatus,
                IsPublic = isPublic,

                StartDateFrom = startDateFrom,
                StartDateTo = startDateTo,
                CreatedAtFrom = createdAtFrom,
                CreatedAtTo = createdAtTo,

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
                return RedirectToAction(nameof(Index));
            }

            group.IsDelete = true;
            group.IsPublic = false;
            group.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "揪團已移至已刪除清單。";
            return RedirectToAction(nameof(Index), new { tab = "active" });
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

            group.IsDelete = false;
            group.IsPublic = true;
            group.ReviewStatus = "正常";
            group.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "揪團已還原至正常清單。";
            return RedirectToAction(nameof(Index), new { tab = "deleted" });
        }
    }
}