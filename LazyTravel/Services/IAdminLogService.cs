using LazyTravel.Models;

namespace LazyTravel.Services
{
    // 這個介面由「檢舉審核台」「Vlog 行程文章」等後台功能共用呼叫,寫進 dbo.AdminLogs 資料表
    public interface IAdminLogService
    {
        Task WriteAsync(string operatorName, string action, string detail, string? targetTable = null, int? targetId = null);

        Task<List<AdminLog>> GetRecentAsync(int take = 50);

        // 依 TargetTable + TargetID 反查(例如查某一筆 Reports 是誰、何時處理的)
        Task<List<AdminLog>> GetForTargetAsync(string targetTable, int targetId);
    }
}
