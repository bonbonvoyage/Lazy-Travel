using LazyTravel.Models.EfModels;

namespace LazyTravel.Models.ViewModels
{
    public class TravelGroupAdminIndexViewModel
    {
        // 用完整命名空間指定,因為 LazyTravel.Models.ViewModels 是 LazyTravel.Models 的子命名空間,
        // 光靠 using LazyTravel.Models.EfModels 沒辦法蓋掉「同名但在外層命名空間」的 LazyTravel.Models.TravelGroup
        public List<LazyTravel.Models.EfModels.TravelGroup> ActiveGroups { get; set; } = new();
        public List<LazyTravel.Models.EfModels.TravelGroup> DeletedGroups { get; set; } = new();

        // 篩選條件
        public string? Keyword { get; set; }
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? OwnerName { get; set; }
        public byte? JoinRule { get; set; }
        public byte? GroupStatus { get; set; }
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