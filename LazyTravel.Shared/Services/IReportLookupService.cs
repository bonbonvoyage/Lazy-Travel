using LazyTravel.Models;

namespace LazyTravel.Services
{
    // 「類型/類別/狀態」的中文對照,改成查資料庫(ReportTargetTypes/ReportReasonCategories/ReportStatuses),
    // 不再寫死在程式碼裡。這三張表資料量小、幾乎不會變動,查一次就快取起來,不用每次都打資料庫。
    public interface IReportLookupService
    {
        string GetTypeName(ReportTargetType type);
        string GetCategoryName(ReportReasonCategory category);
        string GetStatusName(ReportStatus status);

        IReadOnlyList<(ReportTargetType Value, string Name)> GetTypeOptions();
        IReadOnlyList<(ReportReasonCategory Value, string Name)> GetCategoryOptions();
        IReadOnlyList<(ReportStatus Value, string Name)> GetStatusOptions();
    }
}
