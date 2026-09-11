namespace LazyTravel.Shared.Models.DTOs
{
    // MemberProfileService.UpdateProfile() 的回傳型別。原本只回傳 bool，但存檔現在
    // 會做格式驗證（LINE ID／Instagram／Facebook，見 SocialLinkValidator），驗證沒過
    // 時要能把「為什麼失敗」的訊息帶回控制器顯示給使用者看，單純 bool 不夠用。
    //
    // MemberNotFound 另外拉出來，是因為「查無此會員」在控制器那邊應該回 404，
    // 跟「資料格式驗證沒過」應該回 400 是不同的 HTTP 語意，靠這個旗標區分。
    public class ProfileUpdateResult
    {
        public bool Success { get; set; }
        public bool MemberNotFound { get; set; }
        public string? ErrorMessage { get; set; }

        public static ProfileUpdateResult Ok() => new ProfileUpdateResult { Success = true };

        public static ProfileUpdateResult Fail(string message) =>
            new ProfileUpdateResult { Success = false, ErrorMessage = message };

        public static ProfileUpdateResult NotFound(string message) =>
            new ProfileUpdateResult { Success = false, MemberNotFound = true, ErrorMessage = message };
    }
}
