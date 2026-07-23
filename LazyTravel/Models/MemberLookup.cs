namespace LazyTravel.Models;

// Members 資料表／會員後台目前還沒建立，這裡先放一份暫時對照表，
// 讓 Vlog 後台可以顯示「發文會員」姓名、新增/編輯文章時選擇作者。
// 等 Members 模組接上 EF Core 之後，把這裡換成查詢真正的 Members 資料表即可，
// 方法名稱（GetName / IsOfficial）盡量保留一致，改動範圍會比較小。
//
// IsOfficial：官方帳號本質上還是一般會員資料列，只是名稱/顯示上要特別標示（類似認證徽章），
// 跟 Members.Role（一般會員/管理員/超級管理員，權限用）是兩件事，先分開處理。
public static class MemberLookup
{
    public static readonly List<(int MemberID, string Name, bool IsOfficial)> Members = new()
    {
        (1, "阿慢", false),
        (2, "小海", false),
        (3, "阿凱", false),
        (4, "LazyTravel 官方", true),
    };

    public static string GetName(int memberId)
    {
        var match = Members.FirstOrDefault(m => m.MemberID == memberId);
        return match.Name ?? $"會員 #{memberId}";
    }

    public static bool IsOfficial(int memberId) =>
        Members.Any(m => m.MemberID == memberId && m.IsOfficial);
}
