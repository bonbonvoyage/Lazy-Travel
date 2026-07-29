namespace LazyTravel.Areas.Admin.Models
{
    // 各模組操作紀錄的顯示格式,來源是 IAdminLogService 依 TargetTable 篩出來的紀錄
    public class DashboardLogEntry
    {
        public DateTime CreatedAt { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
    }

    public class DashboardModuleCard
    {
        public string Label { get; set; } = string.Empty;
        public string Href { get; set; } = "#";
        public string StatLabel { get; set; } = string.Empty;
        public int StatValue { get; set; }
        // 卡片右上角的數字徽章,只有 >0 才顯示(如待處理檢舉數);用不到就留 null
        public int? Badge { get; set; }
        // 對應 Index.cshtml 裡的 .mod-card.c-xxx 配色 class(member / article / group / report)
        public string ColorKey { get; set; } = "member";
        public List<DashboardLogEntry> RecentLogs { get; set; } = new();
        // 看得到這張卡所需的 PermissionCode,對應 _AdminLayout 側欄同一組權限
        public string RequiredPermission { get; set; } = string.Empty;
    }

    public class DashboardViewModel
    {
        // 公告模組還沒建置,先固定顯示提示文字
        public string NoticeMessage { get; set; } = "目前沒有公告內容。之後接上「公告與通知」模組後,會顯示最新一則系統公告。";
        public List<DashboardModuleCard> Modules { get; set; } = new();
    }
}
