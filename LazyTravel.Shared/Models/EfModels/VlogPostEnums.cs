// 🌟 這個檔案是手寫的，故意不放進反向工程會覆蓋的 VlogPost.cs / ItineraryNode.cs 裡。
// 這三個 enum（VlogPostStatus / VlogMediaType / TravelGroupSize）原本是直接手動加在
// 反向工程產生的 VlogPost.cs 檔案裡，結果這次資料庫重建 + 重新反向工程時被覆蓋掉了，
// 才會一路炸出 VlogPostStore.cs 一大堆「找不到型別」的錯。
// 改放獨立檔案就是為了避免同樣的事再發生一次：以後不管重新反向工程幾次，
// 這個檔案不會被 EF Core Power Tools 動到，VlogPost.cs / ItineraryNode.cs 那兩個屬性型別
// 才需要每次重新反向工程後手動改回 enum（型別本身、property 型別）；
// enum 的定義本身則不用再補了。
namespace LazyTravel.Shared.Models.EfModels;

// 對應規格書「模組 C - 12. 旅遊行程文章主檔 (VlogPosts)」。
// Status 只管「發布/審核」狀態：0=草稿, 1=待審核, 2=已發布。
// 「下架/移除」不算在 Status 裡，一律用 IsDelete 軟刪除表示，兩者互相獨立判斷。
// 資料庫欄位是 tinyint，在 LazyTravelDBContext.OnModelCreating 用 HasConversion<byte>() 轉換。
public enum VlogPostStatus
{
    Draft = 0,
    PendingReview = 1,
    Published = 2,
}

public static class VlogPostStatusExtensions
{
    public static string ToLabel(this VlogPostStatus status) => status switch
    {
        VlogPostStatus.Draft => "草稿",
        VlogPostStatus.PendingReview => "待審核",
        VlogPostStatus.Published => "已發布",
        _ => status.ToString(),
    };

    // 對應 admin.css 既有的 .status-pill 樣式（沒有另外加新樣式：草稿沿用中性的 sand，
    // 待審核借用原本表示風險/需要處理的 coral，已發布維持綠色）
    public static string ToPillClass(this VlogPostStatus status) => status switch
    {
        VlogPostStatus.Draft => "status-pending",
        VlogPostStatus.PendingReview => "status-risk",
        VlogPostStatus.Published => "status-ok",
        _ => "status-pending",
    };
}

// 對應規格書的 MediaType：0=圖片, 1=影片（VlogPosts 與 ItineraryNodes 共用）。
// 資料庫欄位是 tinyint，在 LazyTravelDBContext.OnModelCreating 用 HasConversion<byte>() 轉換。
public enum VlogMediaType
{
    Photo = 0,
    Video = 1,
}

public static class VlogMediaTypeExtensions
{
    public static string ToLabel(this VlogMediaType type) => type switch
    {
        VlogMediaType.Photo => "圖片",
        VlogMediaType.Video => "影片",
        _ => type.ToString(),
    };
}

// 記錄行程適合的旅遊人數區間。
// 🌟 這次資料庫重建後，VlogPosts.TravelPeople 欄位型別從 tinyint 改成 nvarchar(50) 了
// （不是反向工程沒接好，是資料庫欄位本身真的變成存文字），所以改用
// HasConversion<string>() 把這個 enum 存成 "Solo"/"Small"/"Large" 這樣的字串，
// 不再是 HasConversion<byte>()。
public enum TravelGroupSize
{
    Solo = 0,
    Small = 1,
    Large = 2,
}

public static class TravelGroupSizeExtensions
{
    public static string ToLabel(this TravelGroupSize size) => size switch
    {
        TravelGroupSize.Solo => "1人・獨旅",
        TravelGroupSize.Small => "2-4人・精緻團",
        TravelGroupSize.Large => "5人以上・團體",
        _ => size.ToString(),
    };
}

// 🌟 ItineraryNode.StayTime 這次資料庫重建後從 nvarchar(50)（存「1.5 小時」這種文字）
// 改成 int?（分鐘數），這個擴充方法把分鐘數格式化回「N 小時 M 分鐘」給後台顯示用。
public static class ItineraryNodeExtensions
{
    public static string? ToStayLabel(this int? minutes)
    {
        if (!minutes.HasValue || minutes.Value <= 0)
        {
            return null;
        }

        var hours = minutes.Value / 60;
        var mins = minutes.Value % 60;

        var parts = new List<string>();
        if (hours > 0)
        {
            parts.Add($"{hours} 小時");
        }
        if (mins > 0)
        {
            parts.Add($"{mins} 分鐘");
        }

        return string.Join(" ", parts);
    }
}
