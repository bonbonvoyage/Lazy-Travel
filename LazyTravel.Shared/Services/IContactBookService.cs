using LazyTravel.Shared.Models.DTOs;

namespace LazyTravel.Shared.Services
{
    // 通訊錄頁面（好友／追蹤中／黑名單／好友申請）專用的服務。
    // 跟 IMemberProfileService 是分開的兩回事：MemberProfileService 只算「一個標籤」
    // （RelationshipToViewer）給個人頁面用，這裡是「完整清單」給通訊錄頁面用。
    //
    // === 好友系統的兩條規則（已跟負責好友功能的組員確認，是正式規則）===
    // 追蹤（Follow）：單向、免審核，一按就成立，沒有「待核准」這個狀態，
    //   所以這裡沒有 ApproveFollow／RejectFollow 這類方法——追蹤本來就不需要核准。
    // 好友（Friendship）：雙向、需要對方審核，見下面 AcceptFriendRequest／DeclineFriendRequest。
    public interface IContactBookService
    {
        // 撈出 memberId 這個人自己的通訊錄（好友/追蹤中/黑名單/好友申請）。
        ContactBookDto GetContactBook(int memberId);

        // 查看「別人」的手帳第二頁專用：只回傳 Friends／Following 兩份清單，
        // Blocked／Requests 固定回空清單——黑名單和好友申請是 targetMemberId 自己的隱私，
        // 而且這兩個分頁籤的動作（解除黑名單／接受或拒絕好友申請）本來就是在處理
        // 「我自己」的關聯，套用在別人身上完全沒有意義，所以後端一律不把這兩份資料
        // 撈出來，不是只靠前端把分頁籤藏起來（前端藏得住 UI，藏不住 API 回傳的資料）。
        // 呼叫端（MemberProfileService）要先自己判斷 ContactVisible 是不是 true
        // 才呼叫這個方法，這裡不重複判斷隱私規則。
        ContactBookDto GetPublicContactBook(int targetMemberId);

        // 開始追蹤某人。memberId 是我自己，followeeId 是我想追蹤的人。
        // 單向、免審核，一律直接生效（Status=1），呼叫端不用再另外檢查有沒有審核。
        bool Follow(int memberId, int followeeId);

        // 取消我對某人的追蹤。memberId 是我自己，followeeId 是我原本追蹤的人。
        bool Unfollow(int memberId, int followeeId);

        // 送出好友申請（寫進 FriendRequests，帶留言，待對方審核）。
        // memberId 是我自己（發送者）；receiverId 是我想加為好友的人；
        // message 是附加留言，可以是 null／空字串（不附留言）。
        bool SendFriendRequest(int memberId, int receiverId, string? message);

        // 解除好友關係（Friendships 是雙向的一列，兩邊都會一起被移除）。
        bool RemoveFriend(int memberId, int friendId);

        // 封鎖某人。memberId 是我自己，blockedId 是我要封鎖的人。
        // 📌 已修正過一次的規則（參考 LINE）：封鎖不會動到既有的好友關係／追蹤關係，
        // 只是不想再看到對方的揪團／文章（內容可見度，屬於 Explore／VlogPost／揪團列表
        // 那幾個模組要另外接的過濾邏輯，目前都還沒做），封鎖期間也不會再讓對方
        // 追蹤/加我好友——這個規則見實作內註解。真的要解除好友/追蹤關係，
        // 要另外呼叫 RemoveFriend／Unfollow，不是靠 Block 自動處理。
        bool Block(int memberId, int blockedId);

        // 解除封鎖。memberId 是原本封鎖別人、要解除封鎖的那個人。
        bool Unblock(int memberId, int blockedId);

        // 好友申請（FriendRequests 表，帶留言）——這是「加好友」這個獨立流程，
        // 跟上面單向、免審核的追蹤完全無關。這組是給會員主頁手帳第二頁的
        // 「好友申請」分頁籤用的。
        // memberId 是收到申請、要處理的人（也就是目前登入的自己）；
        // requesterId 是送出申請、想加 memberId 為好友的那個人。
        bool AcceptFriendRequest(int memberId, int requesterId);
        bool DeclineFriendRequest(int memberId, int requesterId);
    }
}
