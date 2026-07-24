using LazyTravel.Models;

namespace LazyTravel.Services
{
    // 這個介面由「檢舉審核台」呼叫。
    // AdminLogService 已經接上真的 AdminLogs 資料表(2026-07-22),不再是記憶體暫存。
    public interface IAdminLogService
    {
        Task WriteAsync(string operatorName, string action, string detail, string? targetTable = null, int? targetId = null);

        Task<List<AdminLog>> GetRecentAsync(int take = 50);

        // 依 TargetTable + TargetID 反查(例如查某一筆 Reports 是誰、何時處理的)
        Task<List<AdminLog>> GetForTargetAsync(string targetTable, int targetId);

        // 篩選 + 分頁查詢,給檢舉審核台的操作紀錄分頁用。actionScope 是這個模組會寫的動作名稱清單,
        // 用動作名稱界定範圍(而不是 TargetTable),因為同一模組寫的紀錄可能分散在不同 TargetTable
        Task<(List<AdminLog> Data, int TotalCount)> QueryAsync(
            IEnumerable<string> actionScope, string? operatorKeyword, string? detailKeyword, string? action, int page, int pageSize = 10);
    }
}
