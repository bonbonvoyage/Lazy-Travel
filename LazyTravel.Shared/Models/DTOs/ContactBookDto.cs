using System;
using System.Collections.Generic;

namespace LazyTravel.Shared.Models.DTOs
{
    // 通訊錄頁面（/Contacts）用的資料模型：好友／追蹤中／黑名單／好友申請，
    // 四個分頁籤各自一份清單，都是「目前登入的人」跟別人之間的關聯。
    //
    // === 好友系統的兩條規則（已跟負責好友功能的組員確認，是正式規則不是猜測）===
    // 1. 追蹤（Follow / Follows 表）：單向、不需要對方審核，一按就成立。
    //    Status 欄位在資料庫的預設值就是 1（已生效），這個系統裡永遠不會有
    //    「待審核的追蹤」這種狀態，所以這裡沒有、也不需要 PendingApprovals 這種清單。
    //    追蹤別人只能看到對方公開的旅遊文章之類的公開內容，不會拿到通訊錄權限。
    // 2. 好友（Friendship，透過 FriendRequests 表雙向審核）：需要對方核准才成立，
    //    見下面 Requests 清單。核准之後才會在 Friendships 多一筆、才拿得到通訊錄權限
    //    （見 MemberProfileService.GetProfile 裡 ContactVisible 的判斷）。
    public class ContactBookDto
    {
        public List<ContactMemberDto> Friends { get; set; } = new();
        public List<ContactMemberDto> Following { get; set; } = new();

        public List<ContactMemberDto> Blocked { get; set; } = new();

        // 好友申請：來自 FriendRequests 表（帶留言），是「加好友」這整個獨立流程，
        // 跟上面單向、免審核的 Follow 完全無關。RequestStatus 欄位的編碼規則見
        // ContactBookService 裡 FriendRequestStatusAccepted／FriendRequestStatusRejected
        // 那段註解。
        public List<ContactMemberDto> Requests { get; set; } = new();
    }

    // 通訊錄清單裡的一列：只帶列表要顯示的欄位，不是完整的 MemberProfileDto，
    // 避免把電話等隱私欄位也一起撈出來（那些欄位有自己的可視規則，不該在這裡出現）。
    public class ContactMemberDto
    {
        public int MemberId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Mbti { get; set; }
        public string? City { get; set; }

        // 這筆關聯的時間點：好友＝成立時間；追蹤中＝目前狀態更新時間；
        // 待核准／好友申請＝對方送出請求的時間；黑名單＝封鎖時間。
        public DateTime CreatedAt { get; set; }

        // 只有「好友申請」清單會用到：對方送出申請時附的留言（FriendRequests.Message）。
        public string? Message { get; set; }
    }

    // 通訊錄頁面操作用（追蹤、取消追蹤、解除好友、封鎖、解除封鎖、核准／拒絕好友申請）：
    // 一律只需要「對方是誰」，操作的人是誰一律從登入狀態（ICurrentMemberAccessor）讀，
    // 不接受前端指定，避免有人竄改 request 去操作別人的關聯。
    public class ContactActionDto
    {
        public int TargetMemberId { get; set; }
    }

    // 送出好友申請專用：比 ContactActionDto 多一個可選的留言欄位
    // （對應 FriendRequests.Message，機票放大那段「給團長的話」是另一件事，別搞混）。
    public class SendFriendRequestDto
    {
        public int TargetMemberId { get; set; }
        public string? Message { get; set; }
    }
}
