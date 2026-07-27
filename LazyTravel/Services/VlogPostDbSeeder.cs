using LazyTravel.Models;
using LazyTravel.Models.EfModels;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Services;

// 一次性的示範資料匯入：把 VlogPostStore/ItineraryNodeStore/PostInteractionStore 這幾個
// 記憶體假資料倉儲（Controller 已經不再讀它們了）的內容，寫進真的 LazyTravelDB 三張表。
// 只在 VlogPosts 資料表是空的時候才會執行（重開機/資料庫已有資料就不會重複塞），
// 只碰 VlogPosts / ItineraryNodes / PostInteractions 這三張「我負責的」表，不動 Members 等其他表。
public static class VlogPostDbSeeder
{
    public static async Task SeedAsync(LazyTravelDBContext context)
    {
        if (await context.VlogPosts.AnyAsync())
        {
            return;
        }

        // VlogPostStore 的示範資料寫死用 MemberID 1~4 對應 MemberLookup(阿慢/小海/阿凱/LazyTravel官方)。
        // VlogPosts/PostInteractions 都有外鍵指到 Members，全新建置的資料庫 Members 是空的，
        // 直接塞 VlogPosts 會因為外鍵找不到對應會員而整個失敗。這裡先確認 Members 是空的話，
        // 把這 4 筆示範會員建起來——IDENTITY 從空表開始會依序配到 1~4，剛好對上 MemberLookup。
        // Members 已經有資料(例如共用開發資料庫)就不動，直接沿用既有帳號。
        if (!await context.Members.AnyAsync())
        {
            foreach (var (memberId, name, isOfficial) in MemberLookup.Members)
            {
                context.Members.Add(new LazyTravel.Models.EfModels.Member
                {
                    Email = $"demo-member-{memberId}@lazytravel.local",
                    Name = name,
                    CreatedAt = DateTime.Now,
                });
            }
            await context.SaveChangesAsync();
        }

        foreach (var demoPost in VlogPostStore.GetAll())
        {
            var demoNodes = ItineraryNodeStore.GetByPostId(demoPost.PostId);
            var likeCount = PostInteractionStore.GetLikeCount(demoPost.PostId);
            var favoriteCount = PostInteractionStore.GetFavoriteCount(demoPost.PostId);

            var post = new VlogPost
            {
                MemberId = demoPost.MemberId,
                Title = demoPost.Title,
                MediaUrl = demoPost.MediaUrl,
                MediaType = demoPost.MediaType,
                Content = demoPost.Content,
                Destination = demoPost.Destination,
                TravelDays = demoPost.TravelDays,
                TravelPeople = demoPost.TravelPeople,
                TravelDate = demoPost.TravelDate,
                Status = demoPost.Status,
                CreatedAt = demoPost.CreatedAt,
                UpdatedAt = demoPost.UpdatedAt,
                IsDelete = demoPost.IsDelete,
            };
            context.VlogPosts.Add(post);
            await context.SaveChangesAsync(); // 存檔後 post.PostId 才會是資料庫真正配的 IDENTITY 值

            foreach (var demoNode in demoNodes)
            {
                context.ItineraryNodes.Add(new ItineraryNode
                {
                    PostId = post.PostId,
                    DayNumber = demoNode.DayNumber,
                    LocationName = demoNode.LocationName,
                    ArrivalTime = demoNode.ArrivalTime,
                    StayTime = demoNode.StayTime,
                    DepartureTime = demoNode.DepartureTime,
                    MediaUrl = demoNode.MediaUrl,
                    MediaType = demoNode.MediaType,
                    Description = demoNode.Description,
                    Remarks = demoNode.Remarks,
                });
            }

            // PostInteractions 的複合主鍵是 (PostId, MemberId, ActionType)，不能塞重複組合。
            // 假資料倉儲原本是「每個讚一筆、MemberID 用 1~4 輪流」，會撞到唯一鍵，
            // 這裡改成讚/收藏各自用 1..count 當 MemberId（兩種 ActionType 的鍵不會互撞），
            // 純粹是為了讓資料庫塞得進去且讚數/收藏數的「總數」跟原本假資料一致，不代表真的有這麼多會員。
            for (var m = 1; m <= likeCount; m++)
            {
                context.PostInteractions.Add(new PostInteraction { PostId = post.PostId, MemberId = m, ActionType = PostInteractionType.Like });
            }
            for (var m = 1; m <= favoriteCount; m++)
            {
                context.PostInteractions.Add(new PostInteraction { PostId = post.PostId, MemberId = m, ActionType = PostInteractionType.Favorite });
            }

            await context.SaveChangesAsync();
        }
    }
}
