using System.ComponentModel.DataAnnotations;
using LazyTravel.Shared.Models.DTOs;
using LazyTravel.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LazyTravel.Controllers;

public class MembersController(
    IMemberProfileService memberProfileService,
    ICurrentMemberAccessor currentMemberAccessor) : Controller
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
}

