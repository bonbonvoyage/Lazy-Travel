// 🌟 這個檔案是手寫的，故意不放進反向工程會覆蓋的 PostInteraction.cs 裡（同 VlogPostEnums.cs 的做法）。
// 這個 enum 原本直接手動加在反向工程產生的 PostInteraction.cs 檔案裡，這次資料庫重建 + 重新
// 反向工程時被覆蓋掉了，才會炸出 PostInteractionStore.cs / VlogPostDbSeeder.cs 一堆
// 「名稱 'PostInteractionType' 不存在」的錯。改放獨立檔案避免以後重新反向工程再遺失一次。
// 資料庫欄位是 tinyint，在 LazyTravelDBContext.OnModelCreating 用 HasConversion<byte>() 轉換。
namespace LazyTravel.Shared.Models.EfModels;

public enum PostInteractionType
{
    Like = 1,
    Favorite = 2,
}
