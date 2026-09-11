using System.Text.RegularExpressions;
using LazyTravel.Shared.Models.EfModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Controllers;

[Route("TravelGroups")]
public sealed class AiTravelMatchController : Controller
{
    private static readonly string[] SeaRequestTerms = { "海", "海邊", "海景", "沙灘", "海岸", "浮潛", "水上" };
    private static readonly string[] SeaContentTerms = { "海", "海邊", "海景", "沙灘", "海岸", "七星潭", "清水斷崖", "石梯坪", "白沙灣", "南灣", "浮潛", "水上活動" };

    private readonly LazyTravelDBContext _context;

    public AiTravelMatchController(LazyTravelDBContext context)
    {
        _context = context;
    }

    [HttpPost("AiMatch")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Match([FromForm] string? prompt)
    {
        prompt = prompt?.Trim();
        if (string.IsNullOrWhiteSpace(prompt) || prompt.Length < 4)
        {
            return BadRequest(new { message = "請輸入更完整的旅行需求。" });
        }

        if (prompt.Length > 500)
        {
            return BadRequest(new { message = "旅行需求請控制在 500 字以內。" });
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var groups = await _context.TravelGroups.AsNoTracking()
            .Where(g => g.IsPublic && !g.IsDelete &&
                        g.ReviewStatus == TravelGroupReviewStatus.Normal &&
                        g.CurrentPeople < g.MaxPeople &&
                        g.GroupStatus != 3 &&
                        (!g.EndDate.HasValue || g.EndDate.Value >= today))
            .Include(g => g.TravelGroupTags)
            .Include(g => g.TravelGroupItineraryItems)
            .Include(g => g.TravelGroupImages)
            .Include(g => g.GroupMembers)
                .ThenInclude(gm => gm.Member)
            .AsSplitQuery()
            .ToListAsync();

        var intent = ParseIntent(prompt, groups.Select(g => g.Country));
        var matches = groups
            .Select(group => Score(group, intent, today))
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.DaysDifference)
            .ThenBy(match => match.Group.StartDate)
            .Take(2)
            .Select(match => ToResult(match, intent, today))
            .ToList();

        return Json(new
        {
            criteria = BuildCriteria(intent),
            matches,
            message = matches.Count == 0
                ? "目前沒有符合需求的公開旅團，可以修改條件後再次搜尋。"
                : $"根據你的需求，找到 {matches.Count} 個合適旅團",
        });
    }

    private static TravelIntent ParseIntent(string prompt, IEnumerable<string?> availableCountries)
    {
        var dayMatch = Regex.Match(prompt, @"(?<days>\d{1,2}|[一二三四五六七八九十兩]+)\s*天");
        var days = dayMatch.Success ? ParseNumber(dayMatch.Groups["days"].Value) : null;

        var ageMatch = Regex.Match(prompt, @"(?<min>\d{1,2})\s*(?:[-~～至到])\s*(?<max>\d{1,2})\s*歲");
        int? minAge = ageMatch.Success ? int.Parse(ageMatch.Groups["min"].Value) : null;
        int? maxAge = ageMatch.Success ? int.Parse(ageMatch.Groups["max"].Value) : null;
        if (minAge > maxAge)
        {
            (minAge, maxAge) = (maxAge, minAge);
        }

        var wantsWomen = prompt.Contains("女生", StringComparison.OrdinalIgnoreCase) ||
                         prompt.Contains("女性", StringComparison.OrdinalIgnoreCase);
        var wantsSea = SeaRequestTerms.Any(prompt.Contains);
        var country = availableCountries
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(value => prompt.Contains(value!, StringComparison.OrdinalIgnoreCase));

        return new TravelIntent(prompt, country, days, wantsSea, wantsWomen, minAge, maxAge);
    }

    private static MatchCandidate Score(TravelGroup group, TravelIntent intent, DateOnly today)
    {
        var score = 0;
        var reasons = new List<string>();
        var text = string.Join(' ', new[] { group.GroupTitle, group.Description, group.Country, group.Region }
            .Concat(group.TravelGroupTags.Where(t => !t.IsDelete).Select(t => t.TagName))
            .Concat(group.TravelGroupItineraryItems.SelectMany(i => new[] { i.Title, i.LocationName, i.Description })));

        if (!string.IsNullOrWhiteSpace(intent.Country) &&
            string.Equals(group.Country, intent.Country, StringComparison.OrdinalIgnoreCase))
        {
            score += 25;
            reasons.Add($"目的地符合 {intent.Country}");
        }

        if (intent.WantsSea && SeaContentTerms.Any(text.Contains))
        {
            score += 30;
            reasons.Add(group.Region switch
            {
                "花蓮" => "行程包含七星潭與海岸活動",
                "屏東" => "包含白沙灣與水上活動",
                _ => "行程內容符合看海需求",
            });
        }

        var groupDays = GetDays(group);
        var daysDifference = intent.Days.HasValue && groupDays.HasValue
            ? Math.Abs(intent.Days.Value - groupDays.Value)
            : int.MaxValue;
        if (daysDifference == 0)
        {
            score += 30;
            reasons.Add("旅遊天數符合");
        }
        else if (daysDifference == 1)
        {
            score += 18;
            reasons.Add($"行程比需求{(groupDays > intent.Days ? "多" : "少")} 1 天");
        }

        var activeMembers = group.GroupMembers
            .Where(gm => !gm.IsRemoved && gm.LeftAt == null)
            .Select(gm => gm.Member)
            .Where(member => member is not null)
            .ToList();

        if (activeMembers.Count > 0 && intent.WantsWomen)
        {
            var womenRatio = activeMembers.Count(member => member.Gender == 2) / (double)activeMembers.Count;
            if (womenRatio >= .5)
            {
                score += 8;
            }
        }

        if (activeMembers.Count > 0 && intent.MinAge.HasValue && intent.MaxAge.HasValue)
        {
            var matchingAges = activeMembers.Count(member =>
            {
                var age = GetAge(member.BirthDate, today);
                return age >= intent.MinAge && age <= intent.MaxAge;
            });
            if (matchingAges > 0)
            {
                score += matchingAges == activeMembers.Count ? 12 : 7;
                reasons.Add("團員年齡符合旅伴偏好");
            }
        }

        return new MatchCandidate(group, score, daysDifference, reasons);
    }

    private static object ToResult(MatchCandidate match, TravelIntent intent, DateOnly today)
    {
        var group = match.Group;
        var members = group.GroupMembers
            .Where(gm => !gm.IsRemoved && gm.LeftAt == null && gm.Member is not null)
            .Select(gm => gm.Member)
            .ToList();
        var ages = members
            .Select(member => GetAge(member.BirthDate, today))
            .Where(age => age.HasValue)
            .Select(age => age!.Value)
            .OrderBy(age => age)
            .ToList();
        var womenRatio = members.Count == 0 ? 0 : members.Count(member => member.Gender == 2) / (double)members.Count;
        var companionSummary = members.Count < 2 || ages.Count < 2
            ? "團員資料未達可顯示門檻"
            : $"目前團員年齡主要為 {ages.First()}～{ages.Last()} 歲" +
              (womenRatio >= .5 && intent.WantsWomen ? "，女性占比較高" : "");

        var cover = group.TravelGroupImages
            .Where(image => !image.IsDeleted)
            .OrderByDescending(image => image.IsCover)
            .ThenBy(image => image.SortOrder)
            .Select(image => image.ImageUrl)
            .FirstOrDefault() ?? string.Empty;

        var days = GetDays(group);
        return new
        {
            groupId = group.GroupId,
            title = group.GroupTitle,
            location = string.Join("・", new[] { group.Country, group.Region }.Where(value => !string.IsNullOrWhiteSpace(value))),
            days,
            currentPeople = group.CurrentPeople,
            maxPeople = group.MaxPeople,
            coverImageUrl = cover,
            matchLabel = match.Score >= 70 ? "非常符合" : match.Score >= 40 ? "大致符合" : "部分符合",
            reasons = match.Reasons.Distinct().Take(3).ToArray(),
            companionSummary,
            detailsUrl = $"/TravelGroups/Details/{group.GroupId}",
        };
    }

    private static IReadOnlyList<object> BuildCriteria(TravelIntent intent)
    {
        var criteria = new List<object>();
        if (!string.IsNullOrWhiteSpace(intent.Country)) criteria.Add(new { label = "目的地", value = intent.Country });
        if (intent.WantsSea) criteria.Add(new { label = "旅行興趣", value = "看海、水上活動" });
        if (intent.Days.HasValue) criteria.Add(new { label = "旅遊天數", value = $"{intent.Days} 天" });
        if (intent.MinAge.HasValue && intent.MaxAge.HasValue) criteria.Add(new { label = "旅伴年齡", value = $"{intent.MinAge}～{intent.MaxAge} 歲" });
        if (intent.WantsWomen) criteria.Add(new { label = "旅伴偏好", value = "女性" });
        if (criteria.Count == 0) criteria.Add(new { label = "旅行需求", value = "依照輸入內容搜尋" });
        return criteria;
    }

    private static int? GetDays(TravelGroup group) =>
        group.StartDate.HasValue && group.EndDate.HasValue
            ? group.EndDate.Value.DayNumber - group.StartDate.Value.DayNumber + 1
            : null;

    private static int? GetAge(DateOnly? birthDate, DateOnly today)
    {
        if (!birthDate.HasValue) return null;
        var age = today.Year - birthDate.Value.Year;
        if (birthDate.Value.AddYears(age) > today) age--;
        return age;
    }

    private static int? ParseNumber(string value)
    {
        if (int.TryParse(value, out var number)) return number;
        var digits = new Dictionary<char, int>
        {
            ['一'] = 1, ['二'] = 2, ['兩'] = 2, ['三'] = 3, ['四'] = 4, ['五'] = 5,
            ['六'] = 6, ['七'] = 7, ['八'] = 8, ['九'] = 9,
        };
        if (value == "十") return 10;
        if (value.Contains('十'))
        {
            var parts = value.Split('十');
            var tens = parts[0].Length == 0 ? 1 : digits.GetValueOrDefault(parts[0][0]);
            var ones = parts.Length < 2 || parts[1].Length == 0 ? 0 : digits.GetValueOrDefault(parts[1][0]);
            return tens * 10 + ones;
        }
        return value.Length == 1 && digits.TryGetValue(value[0], out number) ? number : null;
    }

    private sealed record TravelIntent(
        string Prompt,
        string? Country,
        int? Days,
        bool WantsSea,
        bool WantsWomen,
        int? MinAge,
        int? MaxAge);

    private sealed record MatchCandidate(
        TravelGroup Group,
        int Score,
        int DaysDifference,
        IReadOnlyList<string> Reasons);
}
