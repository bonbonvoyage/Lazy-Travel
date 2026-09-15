using LazyTravel.Shared.Models.EfModels;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Services;

internal static class TravelGroupSearch
{
    public static async Task<List<string>> GetDestinationsAsync(IQueryable<TravelGroup> groups)
    {
        var locations = await groups.Select(g => new { g.Country, g.Region }).Distinct().ToListAsync();
        return locations
            .SelectMany(g => new[] { g.Country }.Concat((g.Region ?? "").Split('、', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct()
            .OrderBy(value => value)
            .ToList();
    }
}
