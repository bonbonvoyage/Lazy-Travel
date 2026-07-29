namespace LazyTravel.Services
{
    // TODO(10 黃浚翔):換成真正寫入 Members.Status(2:停權/封鎖)+ 停權到期時間的邏輯
    // 目前先記錄在記憶體 + log,讓「檢舉成立累犯達門檻自動停權」的流程可以被驗證
    public class MemberModerationService : IMemberModerationService
    {
        private readonly ILogger<MemberModerationService> _logger;

        public MemberModerationService(ILogger<MemberModerationService> logger)
        {
            _logger = logger;
        }

        public Task SuspendAsync(string account, int days, string reason)
        {
            _logger.LogWarning("[停權] 帳號 {Account} 停權 {Days} 天,原因:{Reason}", account, days, reason);
            return Task.CompletedTask;
        }
    }
}
