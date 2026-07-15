using LazyTravel.Models;

namespace LazyTravel.Models.ViewModels
{
    public class TravelGroupAdminIndexViewModel
    {
        public List<TravelGroup> ActiveGroups { get; set; } = new();
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

        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }

        public DateTime? CreatedAtFrom { get; set; }
        public DateTime? CreatedAtTo { get; set; }

        // 分頁
        public int ActivePage { get; set; } = 1;
        public int DeletedPage { get; set; } = 1;

        public int ActiveTotalPages { get; set; }
        public int DeletedTotalPages { get; set; }

        public int PageSize { get; set; } = 10;

        // 目前停留在哪個頁籤
        public string Tab { get; set; } = "active";
    }
}