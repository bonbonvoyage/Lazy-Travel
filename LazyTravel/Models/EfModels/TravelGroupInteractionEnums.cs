// 🌟 這個檔案是手寫的，故意不放進反向工程會覆蓋的 TravelGroupInteraction.cs 裡
// （做法比照 PostInteractionEnums.cs／TravelGroupEnums.cs）。
// 對應「25. 揪團互動表 (TravelGroupInteractions)」，跟文章互動表 (PostInteractions) 語意
// 一樣是「按讚/收藏」，但因為是不同的資料表、不同的商業邏輯，所以另外獨立一個 enum，
// 不直接共用 PostInteractionType，避免以後其中一邊要改語意/加選項時互相影響到。
// 資料庫欄位是 tinyint，在 LazyTravelDBContext.OnModelCreating 用 HasConversion<byte>() 轉換。
// 每次重新反向工程都不會動到這個檔案，但 TravelGroupInteraction.cs 的 ActionType 型別
// 還是要記得每次重新反向工程後手動改回這個 enum。
namespace LazyTravel.Shared.Models.EfModels;

public enum TravelGroupInteractionType
{
    Like = 1,
    Favorite = 2,
}

public static class TravelGroupInteractionTypeExtensions
{
    public static string ToLabel(this TravelGroupInteractionType type) => type switch
    {
        TravelGroupInteractionType.Like => "按讚",
        TravelGroupInteractionType.Favorite => "收藏",
        _ => type.ToString(),
    };
}
