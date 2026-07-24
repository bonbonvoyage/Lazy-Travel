using LazyTravel.Models.DTOs;
using LazyTravel.Models.Services;

namespace LazyTravel.Services
{
    // 接上 10 黃浚翔的正式 IMemberService.EditMember。直接用 MemberID 呼叫,不再透過 Email 反查。
    // 商業邏輯(要不要重置暱稱/簡介、要不要另外發通知)還是集中在 MemberService 那一層,這裡只負責轉接。
    // 已知限制:EditMember 目前沒有「停權天數/到期時間」的概念,只有 Status 狀態值,
    // 天數先併入 AdminReason 文字紀錄,等 MemberEditDto 補上到期時間欄位後再改成真的寫入。
    // 注意:MemberService.EditMember 內部把 "{MemberID}|{AdminReason}" 塞進 AdminAuditLogs.TargetID(varchar(50)),
    // 這裡的 AdminReason 一定要夠短,不然會噴 SqlException(字串或二進位資料將會截斷)。
    // 完整的停權原因不會因此遺失,已經另外安全地寫進我們自己的 AdminLogs.Description(nvarchar(300))。
    public class MemberModerationService : IMemberModerationService
    {
        private readonly IMemberService _memberService;
        private const byte SuspendedStatus = 2;

        public MemberModerationService(IMemberService memberService)
        {
            _memberService = memberService;
        }

        public Task SuspendAsync(int memberId, int days, string reason)
        {
            _memberService.EditMember(new MemberEditDto
            {
                MemberID = memberId,
                Status = SuspendedStatus,
                ResetName = false,
                ResetBio = false,
                RemoveAvatar = false,
                // 檢舉審核台自己會呼叫 INotificationService 發停權通知,這裡不用重複發一次
                SendNotification = false,
                AdminReason = $"檢舉審核台自動停權({days}天)"
            });

            return Task.CompletedTask;
        }
    }
}
