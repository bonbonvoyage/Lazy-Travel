using LazyTravel.Shared.Models.DTOs;

namespace LazyTravel.Shared.Services
{
    // 這個介面由「檢舉審核台」「Vlog 行程文章」等後台功能共用呼叫,寫進 dbo.AdminAuditLogs 資料表
    public interface IAdminLogService
    {
        // employeeId 是登入員工的真實 EmployeeID(取自 Admin/AuthController 登入時寫進的 ClaimTypes.NameIdentifier),
        // 直接對應 AdminAuditLogs.EmployeeId 外鍵,不再用姓名反查/佔位會員
        Task WriteAsync(int employeeId, string action, string detail, string? targetResource = null, string? targetId = null);

        Task<List<AdminLogDto>> GetRecentAsync(int take = 50);

        // 依 TargetResource + TargetId 反查(例如查某一筆 Reports 是誰、何時處理的)
        Task<List<AdminLogDto>> GetForTargetAsync(string targetResource, string targetId);

        // 篩選 + 分頁查詢,給檢舉審核台的操作紀錄分頁用。actionScope 是這個模組會寫的動作名稱清單,
        // 用動作名稱界定範圍(而不是 TargetResource),因為同一模組寫的紀錄可能分散在不同 TargetResource
        Task<(List<AdminLogDto> Data, int TotalCount)> QueryAsync(
            IEnumerable<string> actionScope, string? operatorKeyword, string? detailKeyword, string? action, int page, int pageSize = 10);
    }
}
