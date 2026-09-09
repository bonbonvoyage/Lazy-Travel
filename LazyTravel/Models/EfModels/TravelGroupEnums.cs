// 🌟 這個檔案是手寫的，故意不放進反向工程會覆蓋的 TravelGroup.cs 裡（同 VlogPostEnums.cs 的做法）。
// TravelGroups.ReviewStatus 這欄，舊版資料庫是 nvarchar(30)，直接存中文字串
// "正常"/"檢舉審核中"/"違規"；這次資料庫重建後欄位型別真的改成 tinyint 了，
// 改用這個 byte-backed enum 對應，数值順序照原本字串陣列宣告的順序給
// （0=Normal/正常, 1=PendingReview/檢舉審核中, 2=Violation/違規）。
// 資料庫欄位轉換設定在 LazyTravelDBContext.OnModelCreating 用 HasConversion<byte>()。
namespace LazyTravel.Shared.Models.EfModels;

public enum TravelGroupReviewStatus
{
    Normal = 0,
    PendingReview = 1,
    Violation = 2,
}

public static class TravelGroupReviewStatusExtensions
{
    public static string ToLabel(this TravelGroupReviewStatus status) => status switch
    {
        TravelGroupReviewStatus.Normal => "正常",
        TravelGroupReviewStatus.PendingReview => "檢舉審核中",
        TravelGroupReviewStatus.Violation => "違規",
        _ => status.ToString(),
    };
}
