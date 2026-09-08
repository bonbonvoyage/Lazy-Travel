using LazyTravel.Shared.Models;
using LazyTravel.Shared.Models.DTOs;
using LazyTravel.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace LazyTravel.Controllers
{
    public class MembersController : Controller
    {
        private readonly IMemberProfileService _memberProfileService;
        private readonly ICurrentMemberAccessor _currentMemberAccessor;
        private readonly IContactBookService _contactBookService;
        private readonly IReportService _reportService;

        public MembersController(
            IMemberProfileService memberProfileService,
            ICurrentMemberAccessor currentMemberAccessor,
            IContactBookService contactBookService,
            IReportService reportService)
        {
            _memberProfileService = memberProfileService;
            _currentMemberAccessor = currentMemberAccessor;
            _contactBookService = contactBookService;
            _reportService = reportService;
        }

        // GET /Members  或  /Members/Profile
        public IActionResult Index() => Profile();

        public IActionResult Profile()
        {
            ViewData["Title"] = "個人頁面";
            return View("Profile");
        }

        // GET /Members/ProfileData         → 看自己的資料
        // GET /Members/ProfileData?id=5    → 看編號 5 那位會員的資料（聯絡方式依隱私規則遮蔽）
        [HttpGet]
        public IActionResult ProfileData(int? id)
        {
            var viewerId = _currentMemberAccessor.GetCurrentMemberId();
            if (viewerId == null)
            {
                return Unauthorized(new { message = "尚未登入，無法查看個人頁面。" });
            }

            var targetId = id ?? viewerId.Value;
            var profile = _memberProfileService.GetProfile(targetId, viewerId);
            if (profile == null)
            {
                return NotFound(new { message = "找不到這個會員。" });
            }

            return Ok(profile);
        }

        // POST /Members/UpdateProfile   body 是 MemberProfileEditDto 的 JSON，只能改自己
        [HttpPost]
        public IActionResult UpdateProfile([FromBody] MemberProfileEditDto dto)
        {
            var viewerId = _currentMemberAccessor.GetCurrentMemberId();
            if (viewerId == null)
            {
                return Unauthorized(new { message = "尚未登入，無法編輯個人頁面。" });
            }

            var result = _memberProfileService.UpdateProfile(viewerId.Value, dto);
            if (result.MemberNotFound)
            {
                return NotFound(new { message = result.ErrorMessage });
            }
            if (!result.Success)
            {
                // LINE ID／Instagram／Facebook 格式或網域驗證沒過（見
                // SocialLinkValidator），400 而不是 500，這是使用者輸入的問題不是伺服器錯誤。
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new { success = true });
        }

        // POST /Members/UpdateTravelDna   body 是 UpdateTravelDnaDto 的 JSON，只能改自己
        [HttpPost]
        public IActionResult UpdateTravelDna([FromBody] UpdateTravelDnaDto dto)
        {
            var viewerId = _currentMemberAccessor.GetCurrentMemberId();
            if (viewerId == null)
            {
                return Unauthorized(new { message = "尚未登入，無法編輯旅遊 DNA。" });
            }

            var success = _memberProfileService.UpdateTravelDna(viewerId.Value, dto.Scores);
            if (!success)
            {
                return NotFound(new { message = "找不到這個會員。" });
            }

            return Ok(new { success = true });
        }

        // POST /Members/UploadAvatar   multipart/form-data，檔案欄位名叫 file
        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var viewerId = _currentMemberAccessor.GetCurrentMemberId();
            if (viewerId == null)
            {
                return Unauthorized(new { message = "尚未登入，無法上傳大頭貼。" });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "沒有收到檔案。" });
            }

            try
            {
                var url = await _memberProfileService.UpdateAvatarAsync(viewerId.Value, file);
                return Ok(new { avatarUrl = url });
            }
            catch (ArgumentException ex)
            {
                // 檔案格式/大小不符合 LocalImageStorageService 的規則
                return BadRequest(new { message = ex.Message });
            }
        }

        // 以下八個是手帳左頁「查看別人頁面」時追蹤／申請好友／封鎖用的動作，
        // 加上手帳第二頁（好友列表／追蹤名單／黑名單／好友申請）用的動作，
        // 邏輯都在 ContactBookService 裡（跟 /Contacts 頁面共用同一份服務），
        // 這裡只是配合 scene.dc.html 既有的 apiUrl() 慣例，一律掛在 /Members 底下。

        // POST /Members/Follow                 body: { targetMemberId }　→ 開始追蹤
        [HttpPost]
        public IActionResult Follow([FromBody] ContactActionDto dto)
        {
            var viewerId = _currentMemberAccessor.GetCurrentMemberId();
            if (viewerId == null) return Unauthorized(new { message = "尚未登入。" });

            var success = _contactBookService.Follow(viewerId.Value, dto.TargetMemberId);
            if (!success) return BadRequest(new { message = "無法追蹤這個人。" });

            return Ok(new { success = true });
        }

        // POST /Members/SendFriendRequest       body: { targetMemberId, message }　→ 送出好友申請
        [HttpPost]
        public IActionResult SendFriendRequest([FromBody] SendFriendRequestDto dto)
        {
            var viewerId = _currentMemberAccessor.GetCurrentMemberId();
            if (viewerId == null) return Unauthorized(new { message = "尚未登入。" });

            var success = _contactBookService.SendFriendRequest(viewerId.Value, dto.TargetMemberId, dto.Message);
            if (!success) return BadRequest(new { message = "無法送出好友申請。" });

            return Ok(new { success = true });
        }

        // POST /Members/Block                  body: { targetMemberId }　→ 封鎖
        [HttpPost]
        public IActionResult Block([FromBody] ContactActionDto dto)
        {
            var viewerId = _currentMemberAccessor.GetCurrentMemberId();
            if (viewerId == null) return Unauthorized(new { message = "尚未登入。" });

            var success = _contactBookService.Block(viewerId.Value, dto.TargetMemberId);
            if (!success) return BadRequest(new { message = "無法封鎖這個人。" });

            return Ok(new { success = true });
        }

        // POST /Members/RemoveFriend          body: { targetMemberId }　→ 解除好友關係
        [HttpPost]
        public IActionResult RemoveFriend([FromBody] ContactActionDto dto)
        {
            var viewerId = _currentMemberAccessor.GetCurrentMemberId();
            if (viewerId == null) return Unauthorized(new { message = "尚未登入。" });

            var success = _contactBookService.RemoveFriend(viewerId.Value, dto.TargetMemberId);
            if (!success) return NotFound(new { message = "目前不是好友關係。" });

            return Ok(new { success = true });
        }

        // POST /Members/Unfollow              body: { targetMemberId }　→ 取消追蹤
        [HttpPost]
        public IActionResult Unfollow([FromBody] ContactActionDto dto)
        {
            var viewerId = _currentMemberAccessor.GetCurrentMemberId();
            if (viewerId == null) return Unauthorized(new { message = "尚未登入。" });

            var success = _contactBookService.Unfollow(viewerId.Value, dto.TargetMemberId);
            if (!success) return NotFound(new { message = "目前沒有追蹤這個人。" });

            return Ok(new { success = true });
        }

        // POST /Members/AcceptFriendRequest   body: { targetMemberId }　→ 核准好友申請
        [HttpPost]
        public IActionResult AcceptFriendRequest([FromBody] ContactActionDto dto)
        {
            var viewerId = _currentMemberAccessor.GetCurrentMemberId();
            if (viewerId == null) return Unauthorized(new { message = "尚未登入。" });

            var success = _contactBookService.AcceptFriendRequest(viewerId.Value, dto.TargetMemberId);
            if (!success) return NotFound(new { message = "找不到這筆好友申請。" });

            return Ok(new { success = true });
        }

        // POST /Members/DeclineFriendRequest  body: { targetMemberId }　→ 拒絕好友申請
        [HttpPost]
        public IActionResult DeclineFriendRequest([FromBody] ContactActionDto dto)
        {
            var viewerId = _currentMemberAccessor.GetCurrentMemberId();
            if (viewerId == null) return Unauthorized(new { message = "尚未登入。" });

            var success = _contactBookService.DeclineFriendRequest(viewerId.Value, dto.TargetMemberId);
            if (!success) return NotFound(new { message = "找不到這筆好友申請。" });

            return Ok(new { success = true });
        }

        // POST /Members/Unblock               body: { targetMemberId }　→ 解除封鎖
        [HttpPost]
        public IActionResult Unblock([FromBody] ContactActionDto dto)
        {
            var viewerId = _currentMemberAccessor.GetCurrentMemberId();
            if (viewerId == null) return Unauthorized(new { message = "尚未登入。" });

            var success = _contactBookService.Unblock(viewerId.Value, dto.TargetMemberId);
            if (!success) return NotFound(new { message = "目前沒有封鎖這個人。" });

            return Ok(new { success = true });
        }

        // POST /Members/ReportMember          multipart/form-data：targetMemberId、reason、evidence（選填檔案）
        // reason 是 scene.dc.html 畫面上固定四個選項其中一個的原文（REPORT_REASONS），
        // 不是自由輸入。實際存檔邏輯在 IReportService.SubmitMemberReportAsync，
        // 已經跟負責會員主頁的組員確認：Reports 表在共用資料庫已經有 ReasonCategory／
        // IsMalicious／TargetTitle／Description／EvidenceUrl 這幾個欄位，可以直接沿用
        // 現成的 Report 相關程式碼，不用另外做一份簡化版。
        //
        // 🔧 這裡原本是 [FromBody]（JSON），但期中版本 /Report/Create 有讓會員上傳檢舉截圖佐證，
        // 這個主頁版的檢舉 Modal 之前漏了這塊，現在補上 Evidence（見 ReportMemberDto）——
        // 因為多了檔案欄位，JSON body 沒辦法夾帶檔案，所以整個動作改收 multipart/form-data
        // （[FromForm]），scene.dc.html 的 submitReportAction 也已經跟著改成送 FormData。
        [HttpPost]
        public async Task<IActionResult> ReportMember([FromForm] ReportMemberDto dto)
        {
            var viewerId = _currentMemberAccessor.GetCurrentMemberId();
            if (viewerId == null) return Unauthorized(new { message = "尚未登入。" });

            if (viewerId.Value == dto.TargetMemberId)
            {
                return BadRequest(new { message = "不能檢舉自己。" });
            }

            if (string.IsNullOrWhiteSpace(dto.Reason))
            {
                return BadRequest(new { message = "請選擇檢舉原因。" });
            }

            var category = MapReasonToCategory(dto.Reason);
            await _reportService.SubmitMemberReportAsync(viewerId.Value, dto.TargetMemberId, category, dto.Reason, dto.Evidence);

            return Ok(new { success = true });
        }

        // 畫面上四個固定選項對應到 ReportReasonCategory 的分類，純粹是給後台篩選用的標籤
        // （實際檢舉原因文字還是完整存進 Reports.Reason，不會因為分類籠統就漏掉細節）。
        // 「詐騙或商業廣告」對應 Spam（比較偏廣告那一半），「不實資訊或假帳號」跟
        // 「冒用他人身分或照片」都比較接近安全疑慮，對應 Fraud，這組對應如果之後
        // 負責檢舉功能的組員有更精準的分類，這裡再調整即可，不影響其他邏輯。
        private static ReportReasonCategory MapReasonToCategory(string reasonLabel) => reasonLabel switch
        {
            "不實資訊或假帳號" => ReportReasonCategory.Fraud,
            "騷擾、不當言詞" => ReportReasonCategory.Harassment,
            "詐騙或商業廣告" => ReportReasonCategory.Spam,
            "冒用他人身分或照片" => ReportReasonCategory.Fraud,
            _ => ReportReasonCategory.Other,
        };
    }
}
