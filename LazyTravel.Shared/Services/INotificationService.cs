namespace LazyTravel.Shared.Services
{
    // 這個介面由「檢舉審核台」呼叫,實作歸屬 14 洪欣茹(Notifications 共用 Service)
    // 目前先放暫時實作(NotificationService)讓畫面可運作,共用 Service 完成後直接抽換即可
    public interface INotificationService
    {
        Task SendAsync(string account, string title, string message);
    }
}
