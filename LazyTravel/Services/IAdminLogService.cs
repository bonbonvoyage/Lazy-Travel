using LazyTravel.Models;

namespace LazyTravel.Services
{
    // 這個介面由「檢舉審核台」呼叫,實作歸屬 14 洪欣茹(AdminLogs 共用 Service)
    // 目前先放暫時實作(AdminLogService)讓畫面可運作,共用 Service 完成後直接抽換即可
    public interface IAdminLogService
    {
        Task WriteAsync(string operatorName, string action, string detail, string? targetTable = null, int? targetId = null);

        Task<List<AdminLog>> GetRecentAsync(int take = 50);

        // 依 TargetTable + TargetID 反查(例如查某一筆 Reports 是誰、何時處理的)
        Task<List<AdminLog>> GetForTargetAsync(string targetTable, int targetId);
    }
}
