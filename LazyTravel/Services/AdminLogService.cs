using LazyTravel.Models;

namespace LazyTravel.Services
{
    // TODO(14 洪欣茹):換成真正寫入 AdminLogs 資料表的邏輯
    // 目前先把每筆紀錄存進記憶體(不只印 log 就消失),讓判定流程可以被完整追溯與驗證
    public class AdminLogService : IAdminLogService
    {
        private static readonly List<AdminLog> _logs = new();
        private static readonly object _lock = new();
        private static int _nextId = 1;

        private readonly ILogger<AdminLogService> _logger;

        public AdminLogService(ILogger<AdminLogService> logger)
        {
            _logger = logger;
        }

        public Task WriteAsync(string operatorName, string action, string detail, string? targetTable = null, int? targetId = null)
        {
            lock (_lock)
            {
                _logs.Add(new AdminLog
                {
                    Id = _nextId++,
                    OperatorName = operatorName,
                    Action = action,
                    TargetTable = targetTable,
                    TargetId = targetId,
                    Detail = detail,
                    CreatedAt = DateTime.Now
                });
            }

            _logger.LogInformation("[操作紀錄] {Operator} 執行 {Action}:{Detail}", operatorName, action, detail);
            return Task.CompletedTask;
        }

        public Task<List<AdminLog>> GetRecentAsync(int take = 50)
        {
            lock (_lock)
            {
                var recent = _logs.OrderByDescending(l => l.CreatedAt).Take(take).ToList();
                return Task.FromResult(recent);
            }
        }

        public Task<List<AdminLog>> GetForTargetAsync(string targetTable, int targetId)
        {
            lock (_lock)
            {
                var matches = _logs
                    .Where(l => l.TargetTable == targetTable && l.TargetId == targetId)
                    .OrderByDescending(l => l.CreatedAt)
                    .ToList();
                return Task.FromResult(matches);
            }
        }
    }
}
