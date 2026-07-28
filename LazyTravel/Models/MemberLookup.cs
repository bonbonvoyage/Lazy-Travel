namespace LazyTravel.Models;

// Members 資料表已經有真的資料了，這裡不再是查會員資料用的假名單。
// Members 清單只留給 VlogPostDbSeeder 在全新資料庫（Members 是空的）時塞示範會員用；
// 真的畫面顯示／權限判斷請直接讀 post.Member（EF 導覽屬性）。
//
// IsOfficial 用 Email 比對，不是用 MemberID——不同資料庫的 IDENTITY 編號可能不一樣（例如全新建置
// vs 現有開發資料庫），Email 是唯一鍵，比較穩定，換資料庫也不會判斷錯。
public static class MemberLookup
{
    // 「LazyTravel 官方」這筆會員資料的 Email，不是真人登入用的帳密（後台登入之後會走 Employees
    // 表，跟這裡無關），純粹用來識別哪一筆 Member 是官方身份。
    public const string OfficialAccountEmail = "official@lazytravel.local";

    // (Email, Name, IsOfficial) — 只給 VlogPostDbSeeder 全新資料庫塞示範會員用
    public static readonly List<(string Email, string Name, bool IsOfficial)> Members = new()
    {
        ("demo-member-1@lazytravel.local", "阿慢", false),
        ("demo-member-2@lazytravel.local", "小海", false),
        ("demo-member-3@lazytravel.local", "阿凱", false),
        (OfficialAccountEmail, "LazyTravel 官方", true),
    };

    public static bool IsOfficial(LazyTravel.Models.EfModels.Member? member) =>
        member?.Email == OfficialAccountEmail;
}
