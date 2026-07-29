namespace LazyTravel.Services
{
    // 這個介面由「檢舉審核台」在檢舉成立且累犯達門檻時呼叫,實作歸屬 10 黃浚翔(會員管理 /Admin/Members)
    // 目前先放暫時實作(MemberModerationService)讓判定流程可以運作,共用 Service 完成後直接抽換即可
    public interface IMemberModerationService
    {
        Task SuspendAsync(string account, int days, string reason);
    }
}
