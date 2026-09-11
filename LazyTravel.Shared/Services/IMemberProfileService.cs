using LazyTravel.Shared.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace LazyTravel.Shared.Services
{
    // 前台「會員個人頁面」專用的服務——跟 IMemberService（後台管理員在用、
    // 參數都要帶 currentAdminId 的那個）是分開的兩回事，不要搞混或共用。
    public interface IMemberProfileService
    {
        // viewerMemberId 是「目前登入、正在看這頁的人」，可以是 null（未登入當陌生人）。
        // targetMemberId 是「頁面上顯示的是誰的資料」。兩個一樣就是看自己。
        MemberProfileDto? GetProfile(int targetMemberId, int? viewerMemberId);

        // 只能改自己的資料，memberId 就是目前登入的那個人。
        // 回傳型別是 ProfileUpdateResult 不是單純 bool：LINE ID／Instagram／
        // Facebook 這三格會做格式與網域驗證（見 SocialLinkValidator 的註解），
        // 驗證沒過時要能把原因帶回控制器顯示給使用者看。
        ProfileUpdateResult UpdateProfile(int memberId, MemberProfileEditDto dto);

        // 旅遊 DNA 拉桿存檔，一樣只能改自己的。
        bool UpdateTravelDna(int memberId, List<TravelDnaScoreItem> scores);

        // 上傳新大頭貼，回傳可以直接放進 <img src> 的網址，同時已經存回資料庫。
        Task<string> UpdateAvatarAsync(int memberId, IFormFile file);
    }
}
