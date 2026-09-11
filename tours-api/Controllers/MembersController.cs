using System.ComponentModel.DataAnnotations;
using LazyTravel.Shared.Models;
using LazyTravel.Shared.Models.DTOs;
using LazyTravel.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LazyTravel.Controllers;

public class MembersController(
    IMemberProfileService memberProfileService,
    ICurrentMemberAccessor currentMemberAccessor,
    // 🐛 追蹤／申請好友／封鎖／檢舉這幾個按鈕，前端 scene.dc.html 一直都有在打
    // /Members/Follow、/Members/Block、/Members/SendFriendRequest、/Members/ReportMember
    // 這些路徑，但這個 Controller 裡卻完全沒有對應的 action——按鈕點下去 fetch 404，
    // 但前端只有 console.warn、沒有任何錯誤提示，所以畫面上看起來就是「沒反應」。
    // 好友/追蹤/封鎖的商業邏輯其實 ContactBookService 早就寫好了（通訊錄頁面在用），
    // 檢舉的商業邏輯 ReportService.SubmitMemberReportAsync 也已經寫好，這裡只是把
    // 這幾個服務接上、開對應的 action，不是重新實作一套。
    IContactBookService contactBookService,
    IReportService reportService) : Controller
{
    [HttpGet]
    public IActionResult CsrfToken([FromServices] Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery) =>
        Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    [HttpGet]
    public IActionResult Profile() => View();

    [HttpGet]
    public IActionResult ProfileData(int? id)
    {
        var viewerId = currentMemberAccessor.GetCurrentMemberId();
        var targetId = id ?? viewerId;
        if (!targetId.HasValue) return Unauthorized(new { message = "請先登入或使用 devMemberId 測試。" });

        var profile = memberProfileService.GetProfile(targetId.Value, viewerId);
        if (profile == null) return NotFound(new { message = "找不到這個會員。" });

        return Ok(profile);
    }

    // 手帳第二頁「搜尋陌生人」用，一定要先登入才能搜（沒有 devMemberId 後門，
    // 因為這支動作代表的是「我」要去加別人好友，不是查看某個特定會員的頁面，
    // 用 viewerId 就好，不用像 ProfileData 那樣另外收 id 參數）。
    [HttpGet]
    public IActionResult SearchMembers(string? q)
    {
        var viewerId = currentMemberAccessor.GetCurrentMemberId();
        if (!viewerId.HasValue) return Unauthorized(new { message = "請先登入。" });

        var results = memberProfileService.SearchMembers(q ?? "", viewerId.Value);
        return Ok(results);
    }

    public sealed class ProfileInput
    {
        [Required, StringLength(50)] public string Name { get; set; } = "";
        public DateTime? BirthDate { get; set; }
        [Range(0, 2)] public byte Gender { get; set; }
        [StringLength(50)] public string? Occupation { get; set; }
        [RegularExpression("^(INTJ|INTP|ENTJ|ENTP|INFJ|INFP|ENFJ|ENFP|ISTJ|ISFJ|ESTJ|ESFJ|ISTP|ISFP|ESTP|ESFP)$")]
        public string? Mbti { get; set; }
        [StringLength(500)] public string? Bio { get; set; }
        [StringLength(100)] public string? City { get; set; }
        // 手機不用簡訊驗證了，開放自己改；格式驗證交給 MemberProfileService.UpdateProfile
        // 裡的 SocialLinkValidator.TryNormalizePhone，這裡不重複做格式檢查。
        [StringLength(20)] public string? Phone { get; set; }
        [StringLength(50)] public string? LineId { get; set; }
        [StringLength(255)] public string? InstagramUrl { get; set; }
        [StringLength(255)] public string? FacebookUrl { get; set; }
        public bool ContactBookPublic { get; set; }
        public int[]? SkillIds { get; set; }
    }

    static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult UpdateProfile([FromBody] ProfileInput input)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(input.Name) || input.BirthDate > DateTime.Today)
            return BadRequest(new { message = "請確認名稱、生日與選填欄位格式。" });

        var memberId = currentMemberAccessor.GetCurrentMemberId();
        if (!memberId.HasValue) return Unauthorized(new { message = "請先登入。" });

        var result = memberProfileService.UpdateProfile(memberId.Value, new MemberProfileEditDto
        {
            Name = input.Name.Trim(),
            BirthDate = input.BirthDate.HasValue ? DateOnly.FromDateTime(input.BirthDate.Value.Date) : null,
            Gender = input.Gender,
            Occupation = Clean(input.Occupation),
            Mbti = Clean(input.Mbti),
            Bio = Clean(input.Bio),
            City = Clean(input.City),
            Phone = Clean(input.Phone),
            LineId = Clean(input.LineId),
            InstagramUrl = Clean(input.InstagramUrl),
            FacebookUrl = Clean(input.FacebookUrl),
            ContactBookPublic = input.ContactBookPublic,
            SkillIds = input.SkillIds?.Distinct().ToList() ?? new List<int>(),
        });

        if (!result.Success) return BadRequest(new { message = result.ErrorMessage });
        return Ok(new { success = true });
    }

    public sealed class DnaInput { public List<TravelDnaScoreItem> Scores { get; set; } = new(); }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult UpdateTravelDna([FromBody] DnaInput input)
    {
        if (!ModelState.IsValid || input.Scores == null || input.Scores.Count > 4 ||
            input.Scores.Select(x => x.DimensionId).Distinct().Count() != input.Scores.Count ||
            input.Scores.Any(x => x.DimensionId < 1 || x.DimensionId > 4 || x.Score > 100))
            return BadRequest(new { message = "DNA 分數需介於 0 與 100，且每個項目只能提交一次。" });

        var memberId = currentMemberAccessor.GetCurrentMemberId();
        if (!memberId.HasValue) return Unauthorized(new { message = "請先登入。" });

        var ok = memberProfileService.UpdateTravelDna(memberId.Value, input.Scores);
        return ok ? Ok(new { success = true }) : NotFound(new { message = "找不到這個會員。" });
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0 || file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "請選擇 5 MB 以內的 PNG 或 JPG 圖片。" });

        var memberId = currentMemberAccessor.GetCurrentMemberId();
        if (!memberId.HasValue) return Unauthorized(new { message = "請先登入。" });

        var avatarUrl = await memberProfileService.UpdateAvatarAsync(memberId.Value, file);
        return Ok(new { avatarUrl });
    }

    // ===================================================================
    // 追蹤／好友／封鎖：查看別人主頁的按鈕、手帳第二頁清單裡的按鈕、搜尋陌生人
    // 結果列的按鈕全部共用這一組 action。全部要求要先登入（用目前登入的自己當
    // 「我」，不接受 devMemberId 後門，跟 SearchMembers 是同一個道理）。
    // ===================================================================

    public sealed class RelationInput
    {
        [Required] public int TargetMemberId { get; set; }
        // 只有 SendFriendRequest 會用到，其他動作忽略這個欄位。
        [StringLength(300)] public string? Message { get; set; }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Follow([FromBody] RelationInput input)
    {
        var viewerId = currentMemberAccessor.GetCurrentMemberId();
        if (!viewerId.HasValue) return Unauthorized(new { message = "請先登入。" });

        var ok = contactBookService.Follow(viewerId.Value, input.TargetMemberId);
        return ok ? Ok(new { success = true }) : BadRequest(new { message = "追蹤失敗，請稍後再試。" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Unfollow([FromBody] RelationInput input)
    {
        var viewerId = currentMemberAccessor.GetCurrentMemberId();
        if (!viewerId.HasValue) return Unauthorized(new { message = "請先登入。" });

        var ok = contactBookService.Unfollow(viewerId.Value, input.TargetMemberId);
        return ok ? Ok(new { success = true }) : BadRequest(new { message = "取消追蹤失敗，請稍後再試。" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SendFriendRequest([FromBody] RelationInput input)
    {
        var viewerId = currentMemberAccessor.GetCurrentMemberId();
        if (!viewerId.HasValue) return Unauthorized(new { message = "請先登入。" });

        var ok = contactBookService.SendFriendRequest(viewerId.Value, input.TargetMemberId, Clean(input.Message));
        return ok ? Ok(new { success = true }) : BadRequest(new { message = "送出好友申請失敗，請稍後再試。" });
    }

    // requesterId 借用 TargetMemberId 這個欄位傳（見前端 respondFriendRequest：
    // body 送的是 { targetMemberId: requesterId }），不是另外開一個欄位。
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult AcceptFriendRequest([FromBody] RelationInput input)
    {
        var viewerId = currentMemberAccessor.GetCurrentMemberId();
        if (!viewerId.HasValue) return Unauthorized(new { message = "請先登入。" });

        var ok = contactBookService.AcceptFriendRequest(viewerId.Value, input.TargetMemberId);
        return ok ? Ok(new { success = true }) : BadRequest(new { message = "處理好友申請失敗，請稍後再試。" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DeclineFriendRequest([FromBody] RelationInput input)
    {
        var viewerId = currentMemberAccessor.GetCurrentMemberId();
        if (!viewerId.HasValue) return Unauthorized(new { message = "請先登入。" });

        var ok = contactBookService.DeclineFriendRequest(viewerId.Value, input.TargetMemberId);
        return ok ? Ok(new { success = true }) : BadRequest(new { message = "處理好友申請失敗，請稍後再試。" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult RemoveFriend([FromBody] RelationInput input)
    {
        var viewerId = currentMemberAccessor.GetCurrentMemberId();
        if (!viewerId.HasValue) return Unauthorized(new { message = "請先登入。" });

        var ok = contactBookService.RemoveFriend(viewerId.Value, input.TargetMemberId);
        return ok ? Ok(new { success = true }) : BadRequest(new { message = "解除好友失敗，請稍後再試。" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Block([FromBody] RelationInput input)
    {
        var viewerId = currentMemberAccessor.GetCurrentMemberId();
        if (!viewerId.HasValue) return Unauthorized(new { message = "請先登入。" });

        var ok = contactBookService.Block(viewerId.Value, input.TargetMemberId);
        return ok ? Ok(new { success = true }) : BadRequest(new { message = "加入黑名單失敗，請稍後再試。" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Unblock([FromBody] RelationInput input)
    {
        var viewerId = currentMemberAccessor.GetCurrentMemberId();
        if (!viewerId.HasValue) return Unauthorized(new { message = "請先登入。" });

        var ok = contactBookService.Unblock(viewerId.Value, input.TargetMemberId);
        return ok ? Ok(new { success = true }) : BadRequest(new { message = "解除黑名單失敗，請稍後再試。" });
    }

    // ===================================================================
    // 檢舉會員：主頁上「檢舉」那個 Modal 用，跟後台小編那套 SubmitAsync 是分開的
    // 簡化版（見 IReportService.SubmitMemberReportAsync 的註解）。截圖佐證是選填，
    // 所以跟 UploadAvatar 一樣用 multipart/form-data，不是 JSON body。
    // ===================================================================

    public sealed class ReportMemberInput
    {
        [Required] public int TargetMemberId { get; set; }
        [Required, StringLength(200)] public string Reason { get; set; } = "";
    }

    // 前端 REPORT_REASONS 是四個固定的中文選項（不是自由輸入），這裡對應到
    // ReportReasonCategory 給後台檢舉審核台篩選用；真正描述用的文字還是原封
    // 不動存進 Reports.Reason，分類只是方便篩選，不影響審核內容。
    static ReportReasonCategory MapReportReason(string reason) => reason switch
    {
        "不實資訊或假帳號" => ReportReasonCategory.Fraud,
        "騷擾、不當言詞" => ReportReasonCategory.Harassment,
        "詐騙或商業廣告" => ReportReasonCategory.Fraud,
        "冒用他人身分或照片" => ReportReasonCategory.Fraud,
        _ => ReportReasonCategory.Other,
    };

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> ReportMember([FromForm] ReportMemberInput input, IFormFile? evidence)
    {
        if (!ModelState.IsValid) return BadRequest(new { message = "請選擇檢舉原因。" });

        var viewerId = currentMemberAccessor.GetCurrentMemberId();
        if (!viewerId.HasValue) return Unauthorized(new { message = "請先登入。" });

        if (evidence != null && evidence.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "佐證截圖請選擇 5 MB 以內的圖片。" });

        await reportService.SubmitMemberReportAsync(
            viewerId.Value, input.TargetMemberId, MapReportReason(input.Reason), input.Reason, evidence);
        return Ok(new { success = true });
    }
}

