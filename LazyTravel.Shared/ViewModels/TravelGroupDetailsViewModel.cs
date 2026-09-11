namespace LazyTravel.Shared.ViewModels;

// 揪團找旅伴的行程詳情頁資料，全部來自真實資料庫查詢，HomeController 同款模式：
// Controller 的 Data() 直接 Ok(vm)，前端 wwwroot/js/travelgroup-details.js 用 fetch 拿到後渲染。
public class TravelGroupDetailsViewModel
{
    public int GroupId { get; set; }
    public string RoomCode { get; set; } = "";
    public string GroupTitle { get; set; } = "";
    public string Description { get; set; } = "";
    public string Country { get; set; } = "";
    public string Region { get; set; } = "";
    public string DateRangeText { get; set; } = "";
    public string DaysNightsText { get; set; } = "";
    public int MinPeople { get; set; }
    public int MaxPeople { get; set; }
    public int CurrentPeople { get; set; }
    public string ReviewStatusText { get; set; } = "";
    public byte GroupStatus { get; set; }
    public string GroupStatusText { get; set; } = "";
    public bool IsExpired { get; set; }
    public bool IsPastEndDate { get; set; }
    public bool CanStartTrip { get; set; }
    public List<string> GalleryImageUrls { get; set; } = new();

    public List<TravelGroupMemberVm> Members { get; set; } = new();
    public TravelGroupApplicationsVm Applications { get; set; } = new();
    public List<TravelGroupItineraryDayVm> ItineraryDays { get; set; } = new();
    public List<TravelGroupBudgetItemVm> BudgetItems { get; set; } = new();
    public decimal BudgetTotalPerPerson { get; set; }
    public List<TravelGroupActivityVm> ActivityLog { get; set; } = new();

    // 目前這個匿名訪客(visitor cookie)跟這個揪團的關係，前端用來決定按鈕要顯示「申請加入」還是「退出揪團」
    public bool ViewerIsOwner { get; set; }
    public bool ViewerIsMember { get; set; }
    public bool ViewerHasPendingRequest { get; set; }
    public bool ViewerHasFavorited { get; set; }
    public int FavoriteCount { get; set; }
    public string ViewerMode { get; set; } = "guest";
}

public class TravelGroupMemberVm
{
    public int MemberId { get; set; }
    public string Name { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public bool IsOwner { get; set; }
}


public class TravelGroupApplicationsVm
{
    public List<TravelGroupApplicationMemberVm> Joined { get; set; } = new();
    public List<TravelGroupApplicationMemberVm> Pending { get; set; } = new();
    public List<TravelGroupApplicationMemberVm> Rejected { get; set; } = new();
}

public class TravelGroupApplicationMemberVm
{
    public int MemberId { get; set; }
    public int? RequestId { get; set; }
    public string Name { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public string? Message { get; set; }
    public string ProfileUrl { get; set; } = "";
    public DateTime? AppliedAt { get; set; }
}
public class TravelGroupItineraryDayVm
{
    public int DayNumber { get; set; }
    public List<TravelGroupItineraryStopVm> Stops { get; set; } = new();
}

public class TravelGroupItineraryStopVm
{
    public string? TimeRangeText { get; set; }
    public string Title { get; set; } = "";
    public string? LocationName { get; set; }
    public string? Description { get; set; }
}

public class TravelGroupBudgetItemVm
{
    public string BudgetName { get; set; } = "";
    public decimal? Amount { get; set; }
    public string CurrencyCode { get; set; } = "";
    public bool IsRequired { get; set; }
}

// 動態歷程：用真實時間戳記組出來的時間軸（揪團建立時間 + 每位成員真正的加入時間），不是假資料。
public class TravelGroupActivityVm
{
    public DateTime When { get; set; }
    public string Text { get; set; } = "";
}

