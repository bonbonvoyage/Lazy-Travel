namespace LazyTravel.Shared.Services
{
    // 「目前登入的是哪個會員」——只有這一個介面，controller/service 都只依賴這個,
    // 不要直接去 HttpContext.User 東翻西找,這樣組長把登入接上之後只要改
    // CurrentMemberAccessor 這一個檔案就好,其他地方完全不用動。
    public interface ICurrentMemberAccessor
    {
        // 沒登入回傳 null
        int? GetCurrentMemberId();
    }
}
