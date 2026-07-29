namespace LazyTravel.Models
{
    // 對應《Lazy Travel 旅遊平台資料表.docx》官方 AdminLogs 表(LogID/AdminID/Action/TargetTable/TargetID/Description/IPAddress/CreatedAt)
    // 實際表結構歸屬 14 洪欣茹(/Admin/AdminLogs);OperatorName 先用字串代稱,等 Members/驗證做好後改成 AdminID(int, FK->Members)
    public class AdminLog
    {
        public int Id { get; set; }
        public string OperatorName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;

        // 對應官方 TargetTable / TargetID,用來反查「這筆操作是針對哪張表的哪一筆」
        public string? TargetTable { get; set; }
        public int? TargetId { get; set; }

        public string Detail { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
