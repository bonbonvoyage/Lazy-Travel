using LazyTravel.Models.EfModels;

namespace LazyTravel.Models.ViewModels
{
	public class TravelGroupAdminIndexViewModel
	{
		// 揪團清單
		public List<TravelGroup> ActiveGroups { get; set; } = new();

		// 異常揪團清單
		public List<TravelGroup> AbnormalGroups { get; set; } = new();

		// 已刪除清單
		public List<TravelGroup> DeletedGroups { get; set; } = new();

		// 異動紀錄清單
		public List<TravelGroupsLog> Logs { get; set; } = new();

		public Dictionary<int, string> LogEmployeeNames { get; set; } = new();

		// 篩選條件
		public string? Keyword { get; set; }

		// 建立時間篩選
		public DateTime? CreatedAtFrom { get; set; }
		public DateTime? CreatedAtTo { get; set; }

		// 分頁
		public int ActivePage { get; set; } = 1;
		public int AbnormalPage { get; set; } = 1;
		public int DeletedPage { get; set; } = 1;
		public int LogPage { get; set; } = 1;

		public int ActiveTotalPages { get; set; }
		public int AbnormalTotalPages { get; set; }
		public int DeletedTotalPages { get; set; }
		public int LogTotalPages { get; set; }

		public int PageSize { get; set; } = 10;

		// 目前所在頁籤：active / abnormal / deleted / log
		public string Tab { get; set; } = "active";

		// 審核狀態單選（唯一保留的篩選分類）
		public string? SelectedReviewStatus { get; set; }

		// 排序
		public string SortOrder { get; set; } = "asc";

		// 儀表板統計
		public int TotalGroupsCount { get; set; }
		public int AbnormalGroupsCount { get; set; }
		public int DeletedGroupsCount { get; set; }
		public int LogsCount { get; set; }
	}
}
