using LazyTravel.Shared.Models.EfModels;
using LazyTravel.Shared.ViewModels;
using LazyTravel.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LazyTravel.Controllers
{
    // 前台「揪團找旅伴」行程詳情頁：目前 tours-api 還沒有會員登入系統，
    // 申請加入/退出揪團先比照 ExploreController 按讚/收藏的做法，用瀏覽器 Cookie 認一個「訪客身分」。
    // 但 GroupMembers/JoinRequests 的 MemberID 有真的外鍵約束（跟 PostInteractions 不一樣），
    // 不能塞隨機數字，所以 Cookie 記的是「這個訪客對應到哪一個示範會員」，第一次來時隨機分配、之後固定。
    public class TravelGroupsController : Controller
    {
        private readonly LazyTravelDBContext _context;

        public TravelGroupsController(LazyTravelDBContext context)
        {
            _context = context;
        }

        private static readonly Dictionary<string, string[]> RegionCountryMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["亞洲"] = new[]
            {
                "台灣", "臺灣", "日本", "韓國", "南韓", "中國", "香港", "澳門", "泰國", "越南", "新加坡",
                "馬來西亞", "印尼", "菲律賓", "柬埔寨", "寮國", "緬甸", "印度", "尼泊爾", "土耳其",
                "阿拉伯聯合大公國", "杜拜", "卡達", "以色列", "約旦"
            },
            ["歐洲"] = new[]
            {
                "英國", "法國", "德國", "義大利", "西班牙", "葡萄牙", "荷蘭", "比利時", "瑞士", "奧地利",
                "捷克", "匈牙利", "波蘭", "希臘", "希臘・聖托里尼", "冰島", "挪威", "瑞典", "芬蘭",
                "丹麥", "愛爾蘭", "克羅埃西亞"
            },
            ["北美洲"] = new[] { "美國", "加拿大", "墨西哥" },
            ["南美洲"] = new[] { "巴西", "阿根廷", "智利", "秘魯", "玻利維亞", "哥倫比亞" },
            ["非洲"] = new[] { "埃及", "摩洛哥", "南非", "肯亞", "坦尚尼亞" },
            ["大洋洲"] = new[] { "澳洲", "紐西蘭", "斐濟" },
            ["南極洲"] = new[] { "南極洲" },
        };

        // GET /TravelGroups/Create —— 前台新增揪團三步驟流程，目前先提供可操作的表單 UI。
        public IActionResult Create()
        {
            ViewData["Title"] = "建立一趟新的旅行";
            return View();
        }

        // GET /TravelGroups/Draft —— 讀取目前訪客最新一筆未公開草稿。
        [HttpGet]
        public async Task<IActionResult> Draft()
        {
            var ownerMemberId = await GetOrCreateVisitorMemberIdAsync();
            var draft = await _context.TravelGroups.AsNoTracking()
                .Include(g => g.TravelGroupItineraryItems)
                .Include(g => g.TravelGroupBudgets)
                .Where(g => g.OwnerMemberId == ownerMemberId && !g.IsDelete && !g.IsPublic && g.GroupStatus == 0)
                .OrderByDescending(g => g.UpdatedAt)
                .FirstOrDefaultAsync();

            if (draft is null)
            {
                return NoContent();
            }

            var days = draft.TravelGroupItineraryItems
                .Where(item => !string.IsNullOrWhiteSpace(item.Description))
                .GroupBy(item => item.DayNumber)
                .OrderBy(group => group.Key)
                .Select(group =>
                {
                    var values = new[] { "", "", "" };
                    foreach (var item in group.OrderBy(x => x.SortOrder))
                    {
                        var slot = item.StartTime?.Hour switch
                        {
                            < 12 => 0,
                            < 18 => 1,
                            _ => 2,
                        };
                        values[slot] = item.Description;
                    }
                    return values;
                })
                .ToList();

            if (days.Count == 0)
            {
                days = new List<string[]> { new[] { "", "", "" }, new[] { "", "", "" }, new[] { "", "", "" } };
            }

            return Json(new
            {
                draftId = draft.GroupId,
                currentStep = 1,
                groupTitle = draft.GroupTitle == "未命名草稿" ? "" : draft.GroupTitle,
                country = draft.Country == "未選擇" ? "" : draft.Country,
                regions = draft.Region == "未填寫"
                    ? Array.Empty<string>()
                    : draft.Region.Split('、', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                dateRange = ToDateRangeValue(draft.StartDate, draft.EndDate),
                description = draft.Description ?? "",
                coverFileName = "",
                supportFileNames = Array.Empty<string>(),
                activeOptions = Array.Empty<string>(),
                people = new[] { draft.MinPeople.ToString(), draft.MaxPeople.ToString() },
                ageMin = "18",
                ageMax = "65",
                days,
                budgets = draft.TravelGroupBudgets
                    .OrderBy(item => item.SortOrder)
                    .Select(item => new
                    {
                        name = item.BudgetName,
                        amount = item.Amount?.ToString("0.##") ?? "",
                        removable = item.BudgetCategory >= 100,
                    })
                    .ToList(),
            });
        }

        // POST /TravelGroups/SaveDraft —— 將新增揪團流程暫存到 TravelGroups 與既有明細表。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft([FromBody] JsonElement payload)
        {
            var form = ReadDraftRequest(payload);
            if (form is null)
            {
                return BadRequest(new { message = "沒有收到草稿資料。" });
            }

            var ownerMemberId = await GetOrCreateVisitorMemberIdAsync();
            var draft = await UpsertTravelGroupDraftAsync(form, ownerMemberId, publish: false);
            await _context.SaveChangesAsync();
            return Json(new { ok = true, draftId = draft.GroupId });
        }

        // POST /TravelGroups/Submit —— 將新增揪團流程正式寫入資料庫並公開到列表。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit([FromBody] JsonElement payload)
        {
            var form = ReadDraftRequest(payload);
            if (form is null)
            {
                return BadRequest(new { message = "沒有收到揪團資料，請重新整理頁面後再試一次。" });
            }

            var validationError = ValidatePublishRequest(form);
            if (validationError is not null)
            {
                return BadRequest(new { message = validationError });
            }

            var ownerMemberId = await GetOrCreateVisitorMemberIdAsync();
            var group = await UpsertTravelGroupDraftAsync(form, ownerMemberId, publish: true);
            await _context.SaveChangesAsync();

            return Json(new
            {
                ok = true,
                groupId = group.GroupId,
                redirectUrl = Url.Action(nameof(Details), "TravelGroups", new { id = group.GroupId }),
            });
        }

        // GET /TravelGroups —— 公開揪團列表，篩選與分頁都在資料庫端完成。
        public async Task<IActionResult> Index(string? country, string? startDate, string? endDate, string scope = "all", int page = 1)
        {
            const int pageSize = 6;
            page = Math.Max(page, 1);
            scope = scope == "recommended" ? "recommended" : "all";

            var publicGroups = _context.TravelGroups.AsNoTracking()
                .Where(g => g.IsPublic && !g.IsDelete && g.ReviewStatus == TravelGroupReviewStatus.Normal);

            var allCountries = await publicGroups
                .Where(g => g.Country != null && g.Country != "")
                .Select(g => g.Country!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            country = country?.Trim();
            var countries = allCountries;

            if (!string.IsNullOrWhiteSpace(country) && !countries.Contains(country))
            {
                country = null;
            }

            var query = publicGroups
                .Include(g => g.OwnerMember)
                .Include(g => g.TravelGroupImages)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(country))
            {
                query = query.Where(g => g.Country == country);
            }

            var hasStart = DateOnly.TryParse(startDate, out var rangeStart);
            var hasEnd = DateOnly.TryParse(endDate, out var rangeEnd);
            if (hasStart && !hasEnd)
            {
                rangeEnd = rangeStart;
                hasEnd = true;
            }
            if (hasStart && hasEnd)
            {
                if (rangeStart > rangeEnd)
                {
                    (rangeStart, rangeEnd) = (rangeEnd, rangeStart);
                }

                query = query.Where(g =>
                    (!g.StartDate.HasValue || g.StartDate.Value <= rangeEnd) &&
                    (!g.EndDate.HasValue || g.EndDate.Value >= rangeStart));

                startDate = rangeStart.ToString("yyyy-MM-dd");
                endDate = rangeEnd.ToString("yyyy-MM-dd");
            }
            else
            {
                startDate = null;
                endDate = null;
            }

            query = scope == "recommended"
                ? query.OrderByDescending(g => g.CurrentPeople).ThenBy(g => g.StartDate)
                : query.OrderBy(g => g.StartDate ?? DateOnly.MaxValue).ThenByDescending(g => g.CreatedAt);

            var totalCount = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            page = Math.Min(page, totalPages);

            var groups = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var cards = groups.Select(g => new TravelGroupCardViewModel
            {
                GroupId = g.GroupId,
                Title = g.GroupTitle,
                Country = g.Country ?? "",
                Region = g.Region ?? "",
                DateText = g.StartDate.HasValue && g.EndDate.HasValue
                    ? $"{g.StartDate:yyyy/MM/dd} - {g.EndDate:yyyy/MM/dd}"
                    : "日期未定",
                CoverImageUrl = g.TravelGroupImages
                    .Where(i => !i.IsDeleted)
                    .OrderByDescending(i => i.IsCover)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault() ?? "",
                CurrentPeople = g.CurrentPeople,
                MaxPeople = g.MaxPeople,
                OwnerName = g.OwnerMember?.Name ?? "旅人",
                OwnerAvatarUrl = g.OwnerMember?.AvatarUrl,
                Status = g.GroupStatus,
                StatusText = ToGroupStatusLabel(g.GroupStatus),
                Tags = new[] { g.Country, g.Region, g.MaxPeople <= 6 ? "小團" : "多人同行" }
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .Take(3)
                    .Select(x => x!)
                    .ToList(),
            }).ToList();

            ViewData["Title"] = "揪團找旅伴";
            return View(new TravelGroupIndexViewModel
            {
                Groups = cards,
                Countries = countries,
                AllCountries = allCountries,
                Country = country,
                StartDate = startDate,
                EndDate = endDate,
                Scope = scope,
                Page = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
            });
        }

        private static string? NormalizeRegion(string? region)
        {
            return !string.IsNullOrWhiteSpace(region) && RegionCountryMap.ContainsKey(region)
                ? region
                : null;
        }

        private static Dictionary<string, List<string>> BuildCountryOptionsByRegion(List<string> availableCountries)
        {
            var available = availableCountries.ToHashSet();
            return RegionCountryMap.ToDictionary(
                pair => pair.Key,
                pair => pair.Value
                    .Where(available.Contains)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList());
        }

        private static List<string> GetCountryOptions(
            string? region,
            List<string> allCountries,
            Dictionary<string, List<string>> countriesByRegion)
        {
            if (!string.IsNullOrWhiteSpace(region) && countriesByRegion.TryGetValue(region, out var countries))
            {
                return countries;
            }

            return allCountries;
        }

        private static readonly JsonSerializerOptions DraftJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private static TravelGroupDraftRequest? ReadDraftRequest(JsonElement payload)
        {
            if (payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<TravelGroupDraftRequest>(payload.GetRawText(), DraftJsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private async Task<TravelGroup> UpsertTravelGroupDraftAsync(
            TravelGroupDraftRequest form,
            int ownerMemberId,
            bool publish)
        {
            var now = DateTime.Now;
            var title = SafeText(form.GroupTitle, 100);
            var country = SafeText(form.Country, 50);
            var regions = (form.Regions ?? new List<string>())
                .Select(x => SafeText(x, 30))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .Take(6)
                .ToList();
            var description = SafeText(form.Description, 1000);
            var (startDate, endDate) = ParseDateRange(form.DateRange);

            var draft = await _context.TravelGroups
                .Include(g => g.TravelGroupItineraryItems)
                .Include(g => g.TravelGroupBudgets)
                .Include(g => g.GroupMembers)
                .Where(g => g.OwnerMemberId == ownerMemberId && !g.IsDelete && !g.IsPublic && g.GroupStatus == 0)
                .OrderByDescending(g => g.UpdatedAt)
                .FirstOrDefaultAsync();

            if (draft is null)
            {
                draft = new TravelGroup
                {
                    OwnerMemberId = ownerMemberId,
                    CreatedAt = now,
                    CurrentPeople = 1,
                    JoinRule = 1,
                    GroupStatus = 0,
                    IsDelete = false,
                    ReviewStatus = TravelGroupReviewStatus.Normal,
                };
                _context.TravelGroups.Add(draft);
            }

            draft.GroupTitle = string.IsNullOrWhiteSpace(title) ? "未命名草稿" : title;
            draft.Description = description;
            draft.Country = string.IsNullOrWhiteSpace(country) ? "未選擇" : country;
            draft.Region = regions.Count == 0 ? "未填寫" : string.Join('、', regions);
            draft.StartDate = startDate;
            draft.EndDate = endDate;
            draft.MinPeople = GetPeopleValue(form.People, 0, 2);
            draft.MaxPeople = Math.Max(draft.MinPeople, GetPeopleValue(form.People, 1, 10));
            draft.IsPublic = publish;
            draft.UpdatedAt = now;

            _context.TravelGroupItineraryItems.RemoveRange(draft.TravelGroupItineraryItems);
            _context.TravelGroupBudgets.RemoveRange(draft.TravelGroupBudgets);

            foreach (var item in BuildItineraryItems(form.Days, now))
            {
                draft.TravelGroupItineraryItems.Add(item);
            }

            foreach (var item in BuildBudgetItems(form.Budgets, now))
            {
                draft.TravelGroupBudgets.Add(item);
            }

            if (publish && !draft.GroupMembers.Any(gm => gm.MemberId == ownerMemberId && !gm.IsRemoved))
            {
                draft.GroupMembers.Add(new GroupMember
                {
                    MemberId = ownerMemberId,
                    MemberRole = 1,
                    JoinedAt = now,
                    CreatedAt = now,
                    IsRemoved = false,
                });
                draft.CurrentPeople = Math.Max(draft.CurrentPeople, 1);
            }

            return draft;
        }

        private static string? ValidatePublishRequest(TravelGroupDraftRequest form)
        {
            if (string.IsNullOrWhiteSpace(form.GroupTitle))
            {
                return "請輸入旅程名稱。";
            }
            if (string.IsNullOrWhiteSpace(form.Country))
            {
                return "請輸入或選擇國家。";
            }
            if (form.Regions is null || form.Regions.All(string.IsNullOrWhiteSpace))
            {
                return "請至少新增一個城市或地區。";
            }
            if (string.IsNullOrWhiteSpace(form.DateRange))
            {
                return "請選擇出發與回程日期。";
            }

            var (startDate, _) = ParseDateRange(form.DateRange);
            return startDate.HasValue ? null : "請選擇有效的日期區間。";
        }

        private static string SafeText(string? value, int maxLength)
        {
            var text = (value ?? "").Trim();
            return text.Length <= maxLength ? text : text[..maxLength];
        }

        private static int GetPeopleValue(List<string>? values, int index, int fallback)
        {
            if (values is null || values.Count <= index || !int.TryParse(values[index], out var value))
            {
                return fallback;
            }

            return Math.Clamp(value, 1, 30);
        }

        private static (DateOnly? StartDate, DateOnly? EndDate) ParseDateRange(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return (null, null);
            }

            var parts = value.Split(" to ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0 || !DateOnly.TryParse(parts[0], out var startDate))
            {
                return (null, null);
            }

            var endDate = parts.Length > 1 && DateOnly.TryParse(parts[1], out var parsedEndDate)
                ? parsedEndDate
                : startDate;

            if (startDate > endDate)
            {
                (startDate, endDate) = (endDate, startDate);
            }

            return (startDate, endDate);
        }

        private static string ToDateRangeValue(DateOnly? startDate, DateOnly? endDate)
        {
            if (!startDate.HasValue)
            {
                return "";
            }

            return endDate.HasValue && endDate.Value != startDate.Value
                ? $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"
                : $"{startDate:yyyy-MM-dd}";
        }

        private static IEnumerable<TravelGroupItineraryItem> BuildItineraryItems(List<List<string>>? days, DateTime now)
        {
            if (days is null)
            {
                yield break;
            }

            var timeSlots = new[]
            {
                new { Label = "早", Start = new TimeOnly(8, 0), End = new TimeOnly(11, 59) },
                new { Label = "中", Start = new TimeOnly(12, 0), End = new TimeOnly(17, 59) },
                new { Label = "晚", Start = new TimeOnly(18, 0), End = new TimeOnly(22, 0) },
            };

            for (var dayIndex = 0; dayIndex < days.Count; dayIndex++)
            {
                var dayValues = days[dayIndex];
                for (var slotIndex = 0; slotIndex < Math.Min(dayValues.Count, timeSlots.Length); slotIndex++)
                {
                    var description = SafeText(dayValues[slotIndex], 1000);
                    if (string.IsNullOrWhiteSpace(description))
                    {
                        continue;
                    }

                    var slot = timeSlots[slotIndex];
                    yield return new TravelGroupItineraryItem
                    {
                        DayNumber = dayIndex + 1,
                        StartTime = slot.Start,
                        EndTime = slot.End,
                        Title = SafeText($"DAY {dayIndex + 1} {slot.Label}", 150),
                        LocationName = "未填寫",
                        Description = description,
                        SortOrder = (dayIndex * 10) + slotIndex,
                        CreatedAt = now,
                        UpdatedAt = now,
                    };
                }
            }
        }

        private static IEnumerable<TravelGroupBudget> BuildBudgetItems(List<TravelGroupDraftBudgetRequest>? budgets, DateTime now)
        {
            if (budgets is null)
            {
                yield break;
            }

            for (var index = 0; index < budgets.Count; index++)
            {
                var item = budgets[index];
                var name = SafeText(item.Name, 100);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                yield return new TravelGroupBudget
                {
                    BudgetCategory = item.Removable ? (byte)100 : (byte)Math.Min(index, 99),
                    BudgetName = name,
                    Amount = decimal.TryParse(item.Amount, out var amount) ? amount : null,
                    CurrencyCode = "TWD",
                    IsRequired = !item.Removable,
                    SortOrder = index,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
            }
        }

        // GET /TravelGroups/Details/5 —— 頁面外殼，資料由 Data() 提供
        public IActionResult Details(int id)
        {
            ViewData["Title"] = "行程詳情";
            ViewData["GroupId"] = id;
            return View();
        }

        // GET /TravelGroups/Data/5
        public async Task<IActionResult> Data(int id)
        {
            var group = await _context.TravelGroups.AsNoTracking()
                .Include(g => g.OwnerMember)
                .Include(g => g.TravelGroupImages)
                .Include(g => g.TravelGroupItineraryItems)
                .Include(g => g.TravelGroupBudgets)
                .Include(g => g.GroupMembers).ThenInclude(gm => gm.Member)
                .FirstOrDefaultAsync(g => g.GroupId == id);

            if (group is null || group.IsDelete)
            {
                return NotFound();
            }

            var visitorMemberId = await GetOrCreateVisitorMemberIdAsync();

            var days = group.EndDate.HasValue && group.StartDate.HasValue
                ? group.EndDate.Value.DayNumber - group.StartDate.Value.DayNumber + 1
                : (int?)null;

            var activeMembers = group.GroupMembers.Where(gm => !gm.IsRemoved).ToList();
            var pendingRequest = await _context.JoinRequests.AsNoTracking()
                .AnyAsync(r => r.GroupId == id && r.MemberId == visitorMemberId && r.RequestStatus == 0);

            var vm = new TravelGroupDetailsViewModel
            {
                GroupId = group.GroupId,
                RoomCode = $"#TG-{group.CreatedAt:yyyy}-{group.GroupId:D4}",
                GroupTitle = group.GroupTitle,
                Description = group.Description,
                Country = group.Country,
                Region = group.Region,
                DateRangeText = group.StartDate.HasValue && group.EndDate.HasValue
                    ? $"{group.StartDate:yyyy/MM/dd} - {group.EndDate:yyyy/MM/dd}"
                    : "",
                DaysNightsText = days.HasValue ? $"共{days}天{days - 1}夜" : "",
                MinPeople = group.MinPeople,
                MaxPeople = group.MaxPeople,
                CurrentPeople = group.CurrentPeople,
                ReviewStatusText = group.ReviewStatus.ToLabel(),
                GroupStatusText = ToGroupStatusLabel(group.GroupStatus),
                GalleryImageUrls = group.TravelGroupImages
                    .Where(i => !i.IsDeleted)
                    .OrderByDescending(i => i.IsCover).ThenBy(i => i.SortOrder)
                    .Select(i => i.ImageUrl)
                    .ToList(),
                Members = activeMembers
                    .OrderByDescending(gm => gm.MemberRole)
                    .Select(gm => new TravelGroupMemberVm
                    {
                        MemberId = gm.MemberId,
                        Name = gm.Member.Name,
                        AvatarUrl = gm.Member.AvatarUrl,
                        IsOwner = gm.MemberRole == 1,
                    }).ToList(),
                ItineraryDays = group.TravelGroupItineraryItems
                    .GroupBy(i => i.DayNumber)
                    .OrderBy(g => g.Key)
                    .Select(g => new TravelGroupItineraryDayVm
                    {
                        DayNumber = g.Key,
                        Stops = g.OrderBy(i => i.SortOrder).Select(i => new TravelGroupItineraryStopVm
                        {
                            TimeRangeText = i.StartTime.HasValue && i.EndTime.HasValue
                                ? $"{i.StartTime:HH:mm} - {i.EndTime:HH:mm}"
                                : null,
                            Title = i.Title,
                            LocationName = i.LocationName,
                            Description = i.Description,
                        }).ToList(),
                    }).ToList(),
                BudgetItems = group.TravelGroupBudgets
                    .OrderBy(b => b.SortOrder)
                    .Select(b => new TravelGroupBudgetItemVm
                    {
                        BudgetName = b.BudgetName,
                        Amount = b.Amount,
                        CurrencyCode = b.CurrencyCode,
                        IsRequired = b.IsRequired,
                    }).ToList(),
                BudgetTotalPerPerson = group.TravelGroupBudgets.Sum(b => b.Amount ?? 0),
                ViewerIsOwner = group.OwnerMemberId == visitorMemberId,
                ViewerIsMember = activeMembers.Any(gm => gm.MemberId == visitorMemberId),
                ViewerHasPendingRequest = pendingRequest,
                ActivityLog = BuildActivityLog(group, activeMembers),
            };

            return Ok(vm);
        }

        // 動態歷程：不是額外的假資料表，是把「揪團建立時間」跟「每位成員真正的加入時間」按時間排序組出來的。
        private static List<TravelGroupActivityVm> BuildActivityLog(TravelGroup group, List<GroupMember> activeMembers)
        {
            var log = new List<TravelGroupActivityVm>
            {
                new() { When = group.CreatedAt, Text = $"{group.OwnerMember.Name} 建立了揪團房間" },
            };
            log.AddRange(activeMembers
                .Where(gm => gm.MemberRole != 1) // 團主的加入時間跟建立時間重複，不用再列一次
                .Select(gm => new TravelGroupActivityVm { When = gm.JoinedAt, Text = $"{gm.Member.Name} 加入了揪團" }));
            return log.OrderBy(a => a.When).ToList();
        }

        private static string ToGroupStatusLabel(byte status) => status switch
        {
            0 => "等待中",
            1 => "已成團",
            2 => "已額滿",
            3 => "已結束",
            _ => "未知狀態",
        };

        // Room export is an independent draft snapshot; exporting again opens the same article.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportArticle(int id)
        {
            var memberId = await GetOrCreateVisitorMemberIdAsync();
            var group = await _context.TravelGroups.Include(g => g.TravelGroupImages)
                .Include(g => g.TravelGroupItineraryItems).FirstOrDefaultAsync(g => g.GroupId == id && !g.IsDelete);
            if (group is null) return NotFound();
            if (group.OwnerMemberId != memberId) return StatusCode(403, "只有團主可以匯出文章。");
            if (group.ReviewStatus != TravelGroupReviewStatus.Normal) return BadRequest("此房間目前無法匯出。");

            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var existing = await _context.VlogPostRoomExports.FirstOrDefaultAsync(e => e.GroupId == id);
            if (existing is not null)
                return Json(new { redirectUrl = Url.Action("Edit", "Explore", new { id = existing.PostId }) });

            var now = DateTime.Now;
            var images = group.TravelGroupImages.Where(i => !i.IsDeleted && i.ImageType == 0)
                .OrderByDescending(i => i.IsCover).ThenBy(i => i.SortOrder).ToList();
            var post = new VlogPost
            {
                MemberId = group.OwnerMemberId, Title = group.GroupTitle,
                Destination = group.Country ?? "", Content = System.Net.WebUtility.HtmlEncode(group.Description ?? ""),
                TravelDate = group.StartDate?.ToDateTime(TimeOnly.MinValue),
                TravelDays = group.StartDate.HasValue && group.EndDate.HasValue
                    ? Math.Max(1, group.EndDate.Value.DayNumber - group.StartDate.Value.DayNumber + 1) : 1,
                TravelPeople = group.CurrentPeople <= 1 ? TravelGroupSize.Solo : group.CurrentPeople <= 4 ? TravelGroupSize.Small : TravelGroupSize.Large,
                MediaUrl = images.FirstOrDefault()?.ImageUrl ?? "", MediaType = VlogMediaType.Photo,
                Status = VlogPostStatus.Draft, CreatedAt = now, UpdatedAt = now,
                ItineraryNodes = group.TravelGroupItineraryItems.OrderBy(i => i.DayNumber).ThenBy(i => i.SortOrder)
                    .Select(i => new ItineraryNode
                    {
                        DayNumber = i.DayNumber,
                        LocationName = (i.LocationName ?? i.Title ?? "")[..Math.Min(100, (i.LocationName ?? i.Title ?? "").Length)],
                        ArrivalTime = i.StartTime, DepartureTime = i.EndTime,
                        Description = (i.LocationName?.Length > 100 ? i.LocationName + "\n" : "") + (i.Description ?? ""),
                        Remarks = i.Title ?? "", MediaUrl = "", MediaType = VlogMediaType.Photo
                    }).ToList(),
                VlogPostImages = images.Select((i, index) => new VlogPostImage
                {
                    ImageUrl = i.ImageUrl, ImageType = i.ImageType, AltText = i.AltText ?? "",
                    IsCover = index == 0, SortOrder = index, CreatedAt = now, UpdatedAt = now,
                    UploadedByMemberId = group.OwnerMemberId
                }).ToList()
            };
            _context.VlogPosts.Add(post);
            _context.VlogPostRoomExports.Add(new VlogPostRoomExport
            {
                Post = post, GroupId = group.GroupId, Country = group.Country ?? "", Region = group.Region ?? "",
                People = group.CurrentPeople, ExportedAt = now
            });
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return Json(new { redirectUrl = Url.Action("Edit", "Explore", new { id = post.PostId }) });
        }
        // POST /TravelGroups/Join/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int id)
        {
            var group = await _context.TravelGroups.FirstOrDefaultAsync(g => g.GroupId == id && !g.IsDelete);
            if (group is null)
            {
                return NotFound();
            }

            var visitorMemberId = await GetOrCreateVisitorMemberIdAsync();

            if (group.OwnerMemberId == visitorMemberId)
            {
                return BadRequest("你是這個揪團的團主。");
            }

            var alreadyMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == id && gm.MemberId == visitorMemberId && !gm.IsRemoved);
            if (alreadyMember)
            {
                return BadRequest("你已經是這個揪團的成員了。");
            }

            var alreadyPending = await _context.JoinRequests
                .AnyAsync(r => r.GroupId == id && r.MemberId == visitorMemberId && r.RequestStatus == 0);
            if (alreadyPending)
            {
                return BadRequest("已經送出過申請，等團主審核。");
            }

            _context.JoinRequests.Add(new JoinRequest
            {
                GroupId = id,
                MemberId = visitorMemberId,
                Message = "想加入這個揪團！",
                RequestStatus = 0,
                CreatedAt = DateTime.Now,
            });
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST /TravelGroups/Leave/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(int id)
        {
            var visitorMemberId = await GetOrCreateVisitorMemberIdAsync();

            var membership = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == id && gm.MemberId == visitorMemberId && !gm.IsRemoved);
            if (membership is null)
            {
                return BadRequest("你不是這個揪團的成員。");
            }
            if (membership.MemberRole == 1)
            {
                return BadRequest("團主不能退出自己的揪團。");
            }

            membership.IsRemoved = true;
            membership.LeftAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok();
        }

        private const string VisitorMemberCookieName = "ltvmid";

        public sealed class TravelGroupDraftRequest
        {
            public int CurrentStep { get; set; }
            public string? GroupTitle { get; set; }
            public string? Country { get; set; }
            public List<string>? Regions { get; set; }
            public string? DateRange { get; set; }
            public string? Description { get; set; }
            public List<string>? SupportFileNames { get; set; }
            public List<string>? ActiveOptions { get; set; }
            public List<string>? People { get; set; }
            public string? AgeMin { get; set; }
            public string? AgeMax { get; set; }
            public List<List<string>>? Days { get; set; }
            public List<TravelGroupDraftBudgetRequest>? Budgets { get; set; }
        }

        public sealed class TravelGroupDraftBudgetRequest
        {
            public string? Name { get; set; }
            public string? Amount { get; set; }
            public bool Removable { get; set; }
        }

        // 訪客身分：Cookie 記住這個瀏覽器對應到資料庫裡哪一位示範會員（排除各揪團團主，避免訪客一來就是團主）。
        private async Task<int> GetOrCreateVisitorMemberIdAsync()
        {
            if (Request.Cookies.TryGetValue(VisitorMemberCookieName, out var raw) && int.TryParse(raw, out var existingId))
            {
                var stillValid = await _context.Users.AnyAsync(m => m.Id == existingId);
                if (stillValid)
                {
                    return existingId;
                }
            }

            var candidateId = await _context.Users.AsNoTracking()
                .Where(m => m.Email != LazyTravel.Shared.Models.MemberLookup.OfficialAccountEmail)
                .OrderBy(m => m.Id)
                .Select(m => m.Id)
                .Skip(1) // 跳過第一位（各揪團預設的團主），訪客預設不是團主
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
    }
}
