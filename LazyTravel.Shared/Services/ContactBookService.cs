using LazyTravel.Shared.Models.DTOs;
using LazyTravel.Shared.Models.EfModels;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Shared.Services
{
    public class ContactBookService : IContactBookService
    {
        private readonly LazyTravelDBContext _context;

        // === 好友系統的兩條規則（已跟負責好友功能的組員確認，是正式規則不是猜測）===
        // 1. 追蹤（Follow）：單向、不需要對方審核，一按就成立——所以 Follows.Status
        //    在資料庫的預設值就是 1（已生效），這個系統裡不會有「待審核的追蹤」這種
        //    狀態，也就沒有 ApproveFollowRequest／RejectFollowRequest 這類方法。
        //    追蹤別人只能看到對方公開的旅遊文章等公開內容，不會拿到通訊錄權限
        //    （見 MemberProfileService.GetProfile 的 ContactVisible 判斷，只有
        //    Friend／GroupMate 才算，Following 不算）。
        //    📌 提醒：資料表文件裡 Follows.Status 原本的定義是 0=待核准、1=已追蹤，
        //    還搭配 Members.IsPrivateAccount（私人帳號）欄位——也就是文件原本似乎
        //    是設計給「私人帳號需要審核才能被追蹤」這個功能用的。目前確認的產品規則
        //    是「追蹤永遠不需要審核」，所以這裡沒有做私人帳號審核這件事；如果之後
        //    真的要做私人帳號功能，Status=0 這個狀態跟核准/拒絕的邏輯要重新加回來。
        // 2. 好友（Friendship）：雙向、需要對方審核，走的是 FriendRequests 表，
        //    見下面 AcceptFriendRequest／DeclineFriendRequest。核准後才會在
        //    Friendships 多一筆，也才拿得到通訊錄權限。
        private const byte FollowStatusAccepted = 1;

        public ContactBookService(LazyTravelDBContext context)
        {
            _context = context;
        }

        // FriendRequests.RequestStatus 的編碼已對照《Lazy Travel 旅遊平台資料表》文件
        // 確認過，跟這裡原本的規劃一致：
        //   0 = 待回覆（尚未審核）
        //   1 = 已核准（FriendRequestStatusAccepted）
        //   2 = 已拒絕（FriendRequestStatusRejected）
        // 判斷「還沒審核」時故意不直接比對 RequestStatus == 0，改用「ReviewedAt 是不是
        // null」——這個欄位名稱本身就是「審核時間」，null 代表還沒審核過，語意更明確，
        // 也不用擔心 RequestStatus 萬一忘了設定值時預設是 0 造成誤判。
        // 其他組員之後如果要在別的地方（例如通知功能）判斷好友申請的狀態，請照這組編碼走。
        private const byte FriendRequestStatusAccepted = 1;
        private const byte FriendRequestStatusRejected = 2;

        public ContactBookDto GetContactBook(int memberId)
        {
            return new ContactBookDto
            {
                Friends = GetFriends(memberId),
                Following = GetFollowing(memberId),
                Blocked = GetBlocked(memberId),
                Requests = GetFriendRequests(memberId),
            };
        }

        public ContactBookDto GetPublicContactBook(int targetMemberId)
        {
            // 只給 Friends／Following，Blocked／Requests 刻意留空——見介面上的註解。
            return new ContactBookDto
            {
                Friends = GetFriends(targetMemberId),
                Following = GetFollowing(targetMemberId),
            };
        }

        private List<ContactMemberDto> GetFriends(int memberId)
        {
            // Friendships 是無向的一列一對（MemberId1／MemberId2 誰在前面不一定），
            // 兩個方向都要查，再從每一列裡挑出「不是我自己的那一個 Id」。
            var rows = _context.Friendships
                .AsNoTracking()
                .Where(f => f.MemberId1 == memberId || f.MemberId2 == memberId)
                .Select(f => new
                {
                    OtherId = f.MemberId1 == memberId ? f.MemberId2 : f.MemberId1,
                    f.CreatedAt,
                })
                .ToList();

            return AttachMembers(rows.Select(r => (r.OtherId, r.CreatedAt)))
                .OrderByDescending(d => d.CreatedAt)
                .ToList();
        }

        private List<ContactMemberDto> GetFollowing(int memberId)
        {
            // 我主動追蹤別人，而且已經生效（不是還在等對方核准的狀態）。
            var rows = _context.Follows
                .AsNoTracking()
                .Where(f => f.FollowerId == memberId && f.Status == FollowStatusAccepted)
                .Select(f => new { OtherId = f.FolloweeId, f.UpdatedAt })
                .ToList();

            return AttachMembers(rows.Select(r => (r.OtherId, r.UpdatedAt)))
                .OrderByDescending(d => d.CreatedAt)
                .ToList();
        }

        private List<ContactMemberDto> GetBlocked(int memberId)
        {
            var rows = _context.Blocks
                .AsNoTracking()
                .Where(b => b.BlockerId == memberId)
                .Select(b => new { OtherId = b.BlockedId, b.CreatedAt })
                .ToList();

            return AttachMembers(rows.Select(r => (r.OtherId, r.CreatedAt)))
                .OrderByDescending(d => d.CreatedAt)
                .ToList();
        }

        private List<ContactMemberDto> GetFriendRequests(int memberId)
        {
            // 別人想加我為好友、還沒審核過——先送出的先審。
            var rows = _context.FriendRequests
                .AsNoTracking()
                .Where(r => r.ReceiverId == memberId && r.ReviewedAt == null)
                .Select(r => new { OtherId = r.RequesterId, r.CreatedAt, r.Message })
                .ToList();

            var ids = rows.Select(r => r.OtherId).Distinct().ToList();
            var members = _context.Users.AsNoTracking()
                .Where(m => ids.Contains(m.Id) && !m.IsDelete)
                .ToDictionary(m => m.Id);

            var result = new List<ContactMemberDto>();
            foreach (var row in rows.OrderBy(r => r.CreatedAt))
            {
                if (!members.TryGetValue(row.OtherId, out var m)) continue; // 對方帳號已被刪除，不顯示

                result.Add(new ContactMemberDto
                {
                    MemberId = m.Id,
                    Name = m.Name,
                    AvatarUrl = m.AvatarUrl,
                    Mbti = m.Mbti,
                    City = m.City,
                    CreatedAt = row.CreatedAt,
                    Message = row.Message,
                });
            }
            return result;
        }

        // 共用：拿一批「對方 Id + 這筆關聯的時間」，去撈對應的會員資料組成 DTO。
        // 統一在這裡處理、統一濾掉已經被刪除的帳號，四個清單的規則才不會各寫各的兜不起來。
        private List<ContactMemberDto> AttachMembers(IEnumerable<(int OtherId, DateTime CreatedAt)> rows)
        {
            var list = rows.ToList();
            var ids = list.Select(r => r.OtherId).Distinct().ToList();

            var members = _context.Users.AsNoTracking()
                .Where(m => ids.Contains(m.Id) && !m.IsDelete)
                .ToDictionary(m => m.Id);

            var result = new List<ContactMemberDto>();
            foreach (var row in list)
            {
                if (!members.TryGetValue(row.OtherId, out var m)) continue; // 對方帳號已被刪除，不顯示

                result.Add(new ContactMemberDto
                {
                    MemberId = m.Id,
                    Name = m.Name,
                    AvatarUrl = m.AvatarUrl,
                    Mbti = m.Mbti,
                    City = m.City,
                    CreatedAt = row.CreatedAt,
                });
            }
            return result;
        }

        public bool Follow(int memberId, int followeeId)
        {
            // 資料表文件備註「Follows 的追蹤者與被追蹤者需要檢查避免自追」，資料庫應該
            // 也有對應的 CHECK 約束，這裡先自己擋掉，不要讓它變成一個等炸例外的請求。
            if (memberId == followeeId) return false;

            // 對方封鎖我的話不能追蹤——這裡只查「對方封鎖我」這個方向，我封鎖對方的情況
            // 由前端的 notBlocked 判斷擋住按鈕（見 scene.dc.html），這裡是後端最後一道防線。
            bool blockedByTarget = _context.Blocks.Any(b => b.BlockerId == followeeId && b.BlockedId == memberId);
            if (blockedByTarget) return false;

            bool already = _context.Follows.Any(f => f.FollowerId == memberId && f.FolloweeId == followeeId);
            if (already) return true; // 已經追蹤過了，視為成功（不用重複新增，也不用回報失敗）

            _context.Follows.Add(new Follow
            {
                FollowerId = memberId,
                FolloweeId = followeeId,
                Status = FollowStatusAccepted, // 追蹤免審核，一律直接生效
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
            });
            _context.SaveChanges();
            return true;
        }

        public bool Unfollow(int memberId, int followeeId)
        {
            var follow = _context.Follows.FirstOrDefault(f =>
                f.FollowerId == memberId && f.FolloweeId == followeeId);
            if (follow == null) return false;

            _context.Follows.Remove(follow);
            _context.SaveChanges();
            return true;
        }

        public bool SendFriendRequest(int memberId, int receiverId, string? message)
        {
            if (memberId == receiverId) return false;

            bool blockedByTarget = _context.Blocks.Any(b => b.BlockerId == receiverId && b.BlockedId == memberId);
            if (blockedByTarget) return false;

            bool alreadyFriends = _context.Friendships.Any(f =>
                (f.MemberId1 == memberId && f.MemberId2 == receiverId) ||
                (f.MemberId1 == receiverId && f.MemberId2 == memberId));
            if (alreadyFriends) return false; // 已經是好友了，不用再申請一次

            bool alreadyPending = _context.FriendRequests.Any(r =>
                r.RequesterId == memberId && r.ReceiverId == receiverId && r.ReviewedAt == null);
            if (alreadyPending) return true; // 已經送過、對方還沒審核，視為成功，不用重複送出

            _context.FriendRequests.Add(new FriendRequest
            {
                RequesterId = memberId,
                ReceiverId = receiverId,
                Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
                RequestStatus = 0, // 待回覆（見上面 FriendRequestStatusAccepted 那段編碼說明）
                CreatedAt = DateTime.Now,
            });
            _context.SaveChanges();
            return true;
        }

        public bool RemoveFriend(int memberId, int friendId)
        {
            var friendship = _context.Friendships.FirstOrDefault(f =>
                (f.MemberId1 == memberId && f.MemberId2 == friendId) ||
                (f.MemberId1 == friendId && f.MemberId2 == memberId));
            if (friendship == null) return false;

            _context.Friendships.Remove(friendship);
            _context.SaveChanges();
            return true;
        }

        public bool Block(int memberId, int blockedId)
        {
            if (memberId == blockedId) return false;

            // 📌 封鎖的規則已跟負責會員主頁的組員重新確認過一次，修正之前「封鎖會自動
            // 解除好友/追蹤關係」的舊規則——參考 LINE 的作法：封鎖不會動到既有的好友、
            // 追蹤關係，兩者是分開的兩件事：
            //   封鎖＝不想再看到對方的揪團／文章這些內容（內容可見度）。這段過濾邏輯要
            //         接在 Explore／VlogPost／揪團列表那幾個查詢裡（查出結果後排除
            //         Blocks 裡雙向的對象），但那幾個模組是其他組員負責、目前還沒整合
            //         進這個分支，所以這裡只能先記錄封鎖關係，等整合之後再由那幾個
            //         模組各自加上「排除 Blocks」的條件，不是這個服務自己能做完的事。
            //   好友／追蹤＝人際關係本身，要不要保留是另一件事。
            // 所以封鎖這裡「只」新增一筆 Blocks 記錄，不會去刪 Friendships／Follows／
            // FriendRequests。真的要解除關係，使用者要自己另外按「解除好友」／
            // 「取消追蹤」（對應 RemoveFriend／Unfollow 這兩個既有方法）。
            bool already = _context.Blocks.Any(b => b.BlockerId == memberId && b.BlockedId == blockedId);
            if (!already)
            {
                _context.Blocks.Add(new Block
                {
                    BlockerId = memberId,
                    BlockedId = blockedId,
                    CreatedAt = DateTime.Now,
                });
                _context.SaveChanges();
            }

            return true;
        }

        public bool Unblock(int memberId, int blockedId)
        {
            var block = _context.Blocks.FirstOrDefault(b =>
                b.BlockerId == memberId && b.BlockedId == blockedId);
            if (block == null) return false;

            _context.Blocks.Remove(block);
            _context.SaveChanges();
            return true;
        }

        public bool AcceptFriendRequest(int memberId, int requesterId)
        {
            var request = _context.FriendRequests.FirstOrDefault(r =>
                r.ReceiverId == memberId && r.RequesterId == requesterId && r.ReviewedAt == null);
            if (request == null) return false;

            request.RequestStatus = FriendRequestStatusAccepted;
            request.ReviewedAt = DateTime.Now;

            // 核准就順便建立好友關係（Friendships 是無向的一列一對）。
            // 理論上兩人不該已經是好友才對，這裡防呆一下避免重複插入炸出例外。
            bool alreadyFriends = _context.Friendships.Any(f =>
                (f.MemberId1 == memberId && f.MemberId2 == requesterId) ||
                (f.MemberId1 == requesterId && f.MemberId2 == memberId));
            if (!alreadyFriends)
            {
                // 資料庫在 Friendships 表上有 CHECK 條件約束要求 MemberId1 < MemberId2
                // （這樣 UQ_Friendships_Pair 那個唯一索引才擋得住重複、方向相反的成對關係）。
                // 這裡兩個人是「誰核准誰的申請」，跟「誰的 Id 比較小」沒有關係，
                // 所以務必自己排序過再塞進去，不能直接把 memberId/requesterId 對號入座，
                // 不然核准方 Id 比對方大時就會直接違反 CHECK 約束炸例外（DbUpdateException）。
                int lowerId = Math.Min(memberId, requesterId);
                int higherId = Math.Max(memberId, requesterId);
                _context.Friendships.Add(new Friendship
                {
                    MemberId1 = lowerId,
                    MemberId2 = higherId,
                    CreatedAt = DateTime.Now,
                });
            }

            _context.SaveChanges();
            return true;
        }

        public bool DeclineFriendRequest(int memberId, int requesterId)
        {
            var request = _context.FriendRequests.FirstOrDefault(r =>
                r.ReceiverId == memberId && r.RequesterId == requesterId && r.ReviewedAt == null);
            if (request == null) return false;

            request.RequestStatus = FriendRequestStatusRejected;
            request.ReviewedAt = DateTime.Now;
            _context.SaveChanges();
            return true;
        }
    }
}
