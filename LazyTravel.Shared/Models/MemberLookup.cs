namespace LazyTravel.Shared.Models;


public static class MemberLookup
{
    
    public const string OfficialAccountEmail = "official@lazytravel.local";

    // (Email, Name, IsOfficial) — 只給 VlogPostDbSeeder 全新資料庫塞示範會員用
    public static readonly List<(string Email, string Name, bool IsOfficial)> Members = new()
    {
        ("demo-member-1@lazytravel.local", "阿慢", false),
        ("demo-member-2@lazytravel.local", "小海", false),
        ("demo-member-3@lazytravel.local", "阿凱", false),
        (OfficialAccountEmail, "LazyTravel 官方", true),
    };

    public static bool IsOfficial(LazyTravel.Shared.Models.EfModels.Member? member) =>
        member?.Email == OfficialAccountEmail;
}
