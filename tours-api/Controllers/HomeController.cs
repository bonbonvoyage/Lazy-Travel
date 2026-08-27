using LazyTravel.Shared.Models.EfModels;
using LazyTravel.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Controllers;

public class HomeController(LazyTravelDBContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var model = new HomeIndexViewModel
        {
            Trips = await context.TravelGroups.AsNoTracking()
                .Where(g => g.IsPublic && !g.IsDelete && g.ReviewStatus == TravelGroupReviewStatus.Normal)
                .OrderByDescending(g => g.CurrentPeople).ThenByDescending(g => g.CreatedAt)
                .Take(8).Select(g => new HomeTrip(g.GroupId, g.GroupTitle, g.StartDate, g.EndDate,
                    g.CurrentPeople, g.MaxPeople, g.TravelGroupImages.Where(i => !i.IsDeleted && i.IsCover)
                        .OrderBy(i => i.SortOrder).Select(i => i.ImageUrl).FirstOrDefault())).ToListAsync(),
            Articles = await context.VlogPosts.AsNoTracking()
                .Where(p => !p.IsDelete && p.Status == VlogPostStatus.Published)
                .OrderByDescending(p => p.PostInteractions.Count).ThenByDescending(p => p.CreatedAt)
                .Take(8).Select(p => new HomeArticle(p.PostId, p.Title, p.MediaUrl, p.Member.Name, p.CreatedAt)).ToListAsync(),
            Countries = await context.TravelGroups.AsNoTracking()
                .Where(g => g.IsPublic && !g.IsDelete && g.ReviewStatus == TravelGroupReviewStatus.Normal)
                .Select(g => g.Country).Distinct().OrderBy(c => c).ToListAsync()
        };
        return View(model);
    }
    public IActionResult Error() => View();
}