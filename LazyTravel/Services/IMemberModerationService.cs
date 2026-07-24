namespace LazyTravel.Services
{
    // 這個介面由「檢舉審核台」在檢舉成立且累犯達門檻時呼叫。
    // MemberModerationService 已經接上 10 黃浚翔的真實 IMemberService.EditMember(2026-07-22)。
    public interface IMemberModerationService
    {
        Task SuspendAsync(int memberId, int days, string reason);
    }
}
