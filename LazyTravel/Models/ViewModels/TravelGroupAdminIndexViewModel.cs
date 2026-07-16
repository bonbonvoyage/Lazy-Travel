using LazyTravel.Models;

namespace LazyTravel.Models.ViewModels
{
    public class TravelGroupAdminIndexViewModel
    {
        // 揪團清單
        public List<TravelGroup> ActiveGroups { get; set; } = new();

        // 已刪除清單
        public List<TravelGroup> DeletedGroups { get; set; } = new();

        // 篩選條件
        public string? Keyword { get; set; }
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? OwnerName { get; set; }
        public string? JoinRule { get; set; }
        public string? GroupStatus { get; set; }
        public string? ReviewStatus { get; set; }
        public bool? IsPublic { get; set; }

        // 行程時間篩選
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }

        // 建立時間篩選
        public DateTime? CreatedAtFrom { get; set; }
        public DateTime? CreatedAtTo { get; set; }

        // 分頁
        public int ActivePage { get; set; } = 1;
        public int DeletedPage { get; set; } = 1;

        public int ActiveTotalPages { get; set; }
        public int DeletedTotalPages { get; set; }

        public int PageSize { get; set; } = 10;

        // 目前所在頁籤：active / deleted
        public string Tab { get; set; } = "active";

        // 複選篩選條件
        public List<string> SelectedJoinRules { get; set; } = new();
        public List<string> SelectedGroupStatuses { get; set; } = new();
        public List<string> SelectedReviewStatuses { get; set; } = new();
        public List<string> SelectedPublicStatuses { get; set; } = new();

        // 排序
        public string SortOrder { get; set; } = "asc";
    }
}