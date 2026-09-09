using Microsoft.Extensions.Logging;

namespace LazyTravel.Shared.Services
{
    // TODO(14 洪欣茹):換成真正寫入 Notifications 資料表 / 推播的邏輯
    // 目前先把每筆通知寫進 log,讓判定流程可以正常執行、可被追溯
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string account, string title, string message)
        {
            _logger.LogInformation("[通知] 給 {Account}:{Title} - {Message}", account, title, message);
            return Task.CompletedTask;
        }
    }
}
