using LazyTravel.Shared.Models.DTOs;
using LazyTravel.Shared.Models.EfModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LazyTravel.Shared.Services
{
    public class MemberProfileService : IMemberProfileService
    {
        private readonly LazyTravelDBContext _context;
        private readonly IImageStorageService _avatarStorage;
        private readonly IContactBookService _contactBookService;

        private const byte ContactVisibilityPublic = 0;

        public MemberProfileService(
            LazyTravelDBContext context,
            [FromKeyedServices("avatar")] IImageStorageService avatarStorage,
            IContactBookService contactBookService)
        {
            _context = context;
            _avatarStorage = avatarStorage;
            _contactBookService = contactBookService;
        }

        public MemberProfileDto? GetProfile(int targetMemberId, int? viewerMemberId)
        {
            var member = _context.Users.AsNoTracking()
                .FirstOrDefault(m => m.Id == targetMemberId && !m.IsDelete);

            if (member == null) return null;

            bool isSelf = viewerMemberId.HasValue && viewerMemberId.Value == targetMemberId;
            string relationship = isSelf ? "Self" : DetermineRelationship(viewerMemberId, targetMemberId);

            bool contactPublic = member.ContactBookVisibility == ContactVisibilityPublic;
            bool contactVisible = isSelf || contactPublic
                || relationship == "Friend" || relationship == "GroupMate";

            // 查看「別人」頁面時，追蹤／申請好友／封鎖這幾個按鈕要顯示成什麼狀態，
            // 只有看別人（!isSelf）而且有登入（viewerMemberId 有值）才需要算。
            bool viewerIsFollowing = false, viewerHasPendingRequest = false;
            bool viewerHasBlocked = false, viewerIsBlockedByTarget = false;
            if (!isSelf && viewerMemberId.HasValue)
            {
                int viewer = viewerMemberId.Value;
                viewerIsFollowing = _context.Follows.Any(f =>
                    f.FollowerId == viewer && f.FolloweeId == targetMemberId);
                viewerHasPendingRequest = _context.FriendRequests.Any(r =>
                    r.RequesterId == viewer && r.ReceiverId == targetMemberId && r.ReviewedAt == null);
                viewerHasBlocked = _context.Blocks.Any(b =>
                    b.BlockerId == viewer && b.BlockedId == targetMemberId);
                viewerIsBlockedByTarget = _context.Blocks.Any(b =>
                    b.BlockerId == targetMemberId && b.BlockedId == viewer);
            }

            int groupsOwnedCount = _context.TravelGroups
                .Count(g => g.OwnerMemberId == targetMemberId && !g.IsDelete);

            // 「旅遊次數」＝參加過（含自己開的團）、且符合下面三個條件的相異團數，
            // 已跟負責會員主頁的組員確認，是正式規則：
            //   1. 沒有被移除（!IsRemoved）
            //   2. 沒有中途自行退出（LeftAt 是 null）
            //   3. 揪團本身沒有中途取消——資料表沒有另外定義「已取消」這個狀態，
            //      跟其他表一樣用 TravelGroups.IsDelete 當作團被取消/下架的標記，
            //      所以這裡另外 Join TravelGroups、篩掉 IsDelete 的團。
            int tripsJoinedCount = _context.GroupMembers
                .Where(gm => gm.MemberId == targetMemberId && !gm.IsRemoved && gm.LeftAt == null)
                .Select(gm => gm.GroupId)
                .Distinct()
                .Join(
                    _context.TravelGroups.Where(g => !g.IsDelete),
                    id => id,
                    g => g.GroupId,
                    (id, g) => id)
                .Count();

            var skillCatalog = _context.TravelSkills
                .Where(s => s.IsActive)
                .OrderBy(s => s.SkillId)
                .Select(s => new SkillOptionDto { SkillId = s.SkillId, SkillName = s.SkillName })
                .ToList();
            int skillTotal = skillCatalog.Count;
            var mySkillNames = _context.MemberSkills
                .Where(ms => ms.MemberId == targetMemberId)
                .Select(ms => ms.Skill.SkillName)
                .ToList();

            var dimensions = _context.TravelDnaDimensions
                .Include(d => d.TravelDnaOptions)
                .OrderBy(d => d.DimensionId)
                .ToList();

            var dnaRows = _context.MemberTravelDnas
                .Where(d => d.MemberId == targetMemberId)
                .ToList();

            var travelDna = dimensions.Select(dim =>
            {
                var row = dnaRows.FirstOrDefault(r => r.DimensionId == dim.DimensionId);
                byte score = row?.Score ?? (byte)50; // 還沒填過就先給中間值
                var option = dim.TravelDnaOptions.FirstOrDefault(o => score >= o.MinScore && score <= o.MaxScore);

                return new TravelDnaDto
                {
                    DimensionId = dim.DimensionId,
                    DimensionName = dim.DimensionName,
                    LeftLabel = dim.LeftLabel,
                    RightLabel = dim.RightLabel,
                    Score = score,
                    OptionName = option?.OptionName,
                };
            }).ToList();

            return new MemberProfileDto
            {
                MemberId = member.Id,
                Name = member.Name,
                AvatarUrl = member.AvatarUrl,
                BirthDate = member.BirthDate,
                Age = member.BirthDate.HasValue ? CalcAge(member.BirthDate.Value) : null,
                Gender = member.Gender,
                Occupation = member.Occupation,
                Mbti = member.Mbti,
                Bio = member.Bio,
                City = member.City,

                SkillCount = mySkillNames.Count,
                SkillTotal = skillTotal,
                SkillNames = mySkillNames,
                SkillCatalog = skillCatalog,

                TravelDna = travelDna,

                GroupsOwnedCount = groupsOwnedCount,
                TripsJoinedCount = tripsJoinedCount,

                IsSelf = isSelf,
                RelationshipToViewer = relationship,
                ContactBookPublic = contactPublic,
                ContactVisible = contactVisible,
                ViewerIsFollowing = viewerIsFollowing,
                ViewerHasPendingFriendRequest = viewerHasPendingRequest,
                ViewerHasBlocked = viewerHasBlocked,
                ViewerIsBlockedByTarget = viewerIsBlockedByTarget,

                // 看不到的話，這幾個欄位在後端就直接是 null，不會撈出來又靠前端藏。
                Phone = contactVisible ? member.PhoneNumber : null,
                LineId = contactVisible ? member.LineId : null,
                InstagramUrl = contactVisible ? member.InstagramUrl : null,
                FacebookUrl = contactVisible ? member.FacebookUrl : null,

                // 手帳第二頁專用（見 MemberProfileDto.ContactBook 註解）：
                //   看自己 → 完整通訊錄（好友/追蹤/黑名單/好友申請）。
                //   看別人、而且有權限看對方通訊錄（跟聯絡方式用同一個 contactVisible 判斷：
                //     對方通訊錄公開，或彼此是好友/同團）→ 只給 Friends／Following
                //     （GetPublicContactBook 裡已經把 Blocked／Requests 濾掉，見那邊的註解）。
                //   看別人、沒有權限 → null，前端顯示「對方的通訊錄未公開」。
                ContactBook = isSelf
                    ? _contactBookService.GetContactBook(targetMemberId)
                    : (contactVisible ? _contactBookService.GetPublicContactBook(targetMemberId) : null),
            };
        }

        public ProfileUpdateResult UpdateProfile(int memberId, MemberProfileEditDto dto)
        {
            var member = _context.Users.FirstOrDefault(m => m.Id == memberId && !m.IsDelete);
            if (member == null) return ProfileUpdateResult.NotFound("找不到這個會員。");

            // 存進資料庫之前，先驗證使用者自己貼的 LINE ID／Instagram／Facebook——
            // 前端 scene.dc.html 的 validateLineId／validateSocialUrl 已經先擋過一次，
            // 但那邊不是唯一防線（有人可能繞過畫面直接打這支 API），這裡才是真正
            // 擋得住的地方。任何一項驗證沒過就整批不存，並把原因帶回去，不要靜靜
            // 存一半或是把壞資料存進去。
            // 手機不用簡訊驗證了（產品規則已確認），一樣先驗證台灣手機格式再存，
            // 邏輯跟 LineId/Instagram/Facebook 這三個完全同一套模式。
            if (!SocialLinkValidator.TryNormalizePhone(dto.Phone, out var normalizedPhone, out var phoneError))
            {
                return ProfileUpdateResult.Fail(phoneError!);
            }
            if (!SocialLinkValidator.TryNormalizeLineId(dto.LineId, out var normalizedLineId, out var lineIdError))
            {
                return ProfileUpdateResult.Fail(lineIdError!);
            }
            if (!SocialLinkValidator.TryNormalizeSocialUrl(dto.InstagramUrl, isInstagram: true, out var normalizedIg, out var igError))
            {
                return ProfileUpdateResult.Fail(igError!);
            }
            if (!SocialLinkValidator.TryNormalizeSocialUrl(dto.FacebookUrl, isInstagram: false, out var normalizedFb, out var fbError))
            {
                return ProfileUpdateResult.Fail(fbError!);
            }

            member.Name = dto.Name;
            member.BirthDate = dto.BirthDate;
            member.Gender = dto.Gender;
            member.Occupation = dto.Occupation;
            member.Mbti = dto.Mbti;
            member.Bio = dto.Bio;
            member.City = dto.City;
            // 存驗證/正規化過後的值（normalized*），不是 dto 上原始未經檢查的內容。
            // 手機號碼變更的話，順便把 PhoneNumberConfirmed 重設成 false——這個欄位
            // 語意是「這支號碼有沒有經過簡訊驗證」，既然現在跳過簡訊驗證直接讓會員
            // 自己改，新填的號碼當然還沒被驗證過，不應該沿用舊號碼留下的已驗證狀態。
            // 目前系統其他地方都還沒有讀這個欄位做任何判斷，這裡只是先把資料語意存對。
            if (normalizedPhone != member.PhoneNumber)
            {
                member.PhoneNumberConfirmed = false;
            }
            member.PhoneNumber = normalizedPhone;
            member.LineId = normalizedLineId;
            member.InstagramUrl = normalizedIg;
            member.FacebookUrl = normalizedFb;
            member.ContactBookVisibility = dto.ContactBookPublic ? (byte)0 : (byte)1;

            // 技能標籤整批覆蓋：先清掉原本的，再照 dto 裡的清單重新加。
            var existingSkills = _context.MemberSkills.Where(ms => ms.MemberId == memberId);
            _context.MemberSkills.RemoveRange(existingSkills);

            foreach (var skillId in dto.SkillIds.Distinct())
            {
                _context.MemberSkills.Add(new MemberSkill
                {
                    MemberId = memberId,
                    SkillId = skillId,
                    CreatedAt = DateTime.Now,
                });
            }

            _context.SaveChanges();
            return ProfileUpdateResult.Ok();
        }

        public bool UpdateTravelDna(int memberId, List<TravelDnaScoreItem> scores)
        {
            var member = _context.Users.FirstOrDefault(m => m.Id == memberId && !m.IsDelete);
            if (member == null) return false;

            foreach (var item in scores)
            {
                var row = _context.MemberTravelDnas
                    .FirstOrDefault(d => d.MemberId == memberId && d.DimensionId == item.DimensionId);

                if (row == null)
                {
                    _context.MemberTravelDnas.Add(new MemberTravelDNA
                    {
                        MemberId = memberId,
                        DimensionId = item.DimensionId,
                        Score = item.Score,
                        UpdatedAt = DateTime.Now,
                    });
                }
                else
                {
                    row.Score = item.Score;
                    row.UpdatedAt = DateTime.Now;
                }
            }

            _context.SaveChanges();
            return true;
        }

        public async Task<string> UpdateAvatarAsync(int memberId, IFormFile file)
        {
            var member = _context.Users.FirstOrDefault(m => m.Id == memberId && !m.IsDelete);
            if (member == null)
            {
                throw new InvalidOperationException("找不到這個會員。");
            }

            var url = await _avatarStorage.UploadAsync(file, "avatars");
            member.AvatarUrl = url;
            _context.SaveChanges();

            return url;
        }

        public List<MemberSearchResultDto> SearchMembers(string keyword, int viewerMemberId)
        {
            var q = (keyword ?? "").Trim();
            if (q.Length == 0) return new List<MemberSearchResultDto>();

            // 封鎖關係不分方向：我封鎖的人、封鎖我的人都不該出現在搜尋結果裡，
            // 直接送好友申請給對方也一定會被擋，不如一開始就不要顯示。
            var blockedByMeIds = _context.Blocks
                .Where(b => b.BlockerId == viewerMemberId)
                .Select(b => b.BlockedId);
            var blockedMeIds = _context.Blocks
                .Where(b => b.BlockedId == viewerMemberId)
                .Select(b => b.BlockerId);

            var candidates = _context.Users.AsNoTracking()
                .Where(m => !m.IsDelete && m.Id != viewerMemberId && m.Name.Contains(q))
                .Where(m => !blockedByMeIds.Contains(m.Id) && !blockedMeIds.Contains(m.Id))
                .OrderBy(m => m.Name)
                .Take(30)
                .Select(m => new { m.Id, m.Name, m.AvatarUrl, m.City, m.Mbti })
                .ToList();

            if (candidates.Count == 0) return new List<MemberSearchResultDto>();

            var candidateIds = candidates.Select(c => c.Id).ToList();

            var friendIds = _context.Friendships
                .Where(f =>
                    (f.MemberId1 == viewerMemberId && candidateIds.Contains(f.MemberId2)) ||
                    (f.MemberId2 == viewerMemberId && candidateIds.Contains(f.MemberId1)))
                .ToList()
                .Select(f => f.MemberId1 == viewerMemberId ? f.MemberId2 : f.MemberId1)
                .ToHashSet();

            var pendingSentIds = _context.FriendRequests
                .Where(r => r.RequesterId == viewerMemberId && r.ReviewedAt == null && candidateIds.Contains(r.ReceiverId))
                .Select(r => r.ReceiverId)
                .ToHashSet();

            var pendingReceivedIds = _context.FriendRequests
                .Where(r => r.ReceiverId == viewerMemberId && r.ReviewedAt == null && candidateIds.Contains(r.RequesterId))
                .Select(r => r.RequesterId)
                .ToHashSet();

            return candidates.Select(c => new MemberSearchResultDto
            {
                MemberId = c.Id,
                Name = c.Name,
                AvatarUrl = c.AvatarUrl,
                City = c.City,
                Mbti = c.Mbti,
                RelationshipStatus =
                    friendIds.Contains(c.Id) ? "Friend"
                    : pendingSentIds.Contains(c.Id) ? "PendingSent"
                    : pendingReceivedIds.Contains(c.Id) ? "PendingReceived"
                    : "Stranger",
            }).ToList();
        }

        private static int CalcAge(DateOnly birthDate)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            int age = today.Year - birthDate.Year;
            if (birthDate > today.AddYears(-age)) age--;
            return age;
        }

        private string DetermineRelationship(int? viewerMemberId, int targetMemberId)
        {
            if (!viewerMemberId.HasValue) return "Stranger"; // 未登入一律當陌生人

            int viewer = viewerMemberId.Value;

            bool isFriend = _context.Friendships.Any(f =>
                (f.MemberId1 == viewer && f.MemberId2 == targetMemberId) ||
                (f.MemberId1 == targetMemberId && f.MemberId2 == viewer));
            if (isFriend) return "Friend";

            if (HasActiveSharedGroup(viewer, targetMemberId)) return "GroupMate";

            // 追蹤（Follow）是單向、不需要對方審核，一按就成立，所以 Follows.Status
            // 在資料庫預設值就是 1（已生效）——已跟負責好友功能的組員確認，是正式規則
            // 不是猜測（同樣的規則也寫在 ContactBookService 裡）。這個判斷只影響
            // RelationshipToViewer 這個顯示用的標籤，不影響通訊錄看不看得到
            // （通訊錄只看好友跟同團，追蹤不給通訊錄權限）。
            bool isFollowing = _context.Follows.Any(f =>
                f.FollowerId == viewer && f.FolloweeId == targetMemberId && f.Status == 1);
            if (isFollowing) return "Following";

            return "Stranger";
        }

        private bool HasActiveSharedGroup(int memberA, int memberB)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            // TravelGroups.GroupStatus 的編碼已對照《Lazy Travel 旅遊平台資料表》文件
            // 確認過：0=等待、1=成行、2=結束——這裡只排除「已結束」(2) 的團，
            // 還在等待成行的團 (0) 跟已經成行的團 (1) 都算「還在進行中」。
            const byte GroupStatusEnded = 2;

            var activeGroupIdsA = _context.GroupMembers
                .Where(gm => gm.MemberId == memberA && !gm.IsRemoved && gm.LeftAt == null)
                .Select(gm => gm.GroupId);

            var activeGroupIdsB = _context.GroupMembers
                .Where(gm => gm.MemberId == memberB && !gm.IsRemoved && gm.LeftAt == null)
                .Select(gm => gm.GroupId);

            // 「揪團結束」同時看兩個條件：EndDate 已過，或 GroupStatus 已經是「結束」。
            return activeGroupIdsA.Intersect(activeGroupIdsB)
                .Join(
                    _context.TravelGroups.Where(g =>
                        !g.IsDelete &&
                        g.GroupStatus != GroupStatusEnded &&
                        (g.EndDate == null || g.EndDate >= today)),
                    id => id,
                    g => g.GroupId,
                    (id, g) => id)
                .Any();
        }
    }
}


