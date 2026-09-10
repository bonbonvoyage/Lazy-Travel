using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LazyTravel.Shared.Models.EfModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Controllers;

[Authorize(AuthenticationSchemes = "MemberAuth")]
public class MembersController(LazyTravelDBContext db) : Controller
{
    [HttpGet]
    public IActionResult CsrfToken([FromServices] Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery) => Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    int CurrentId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    public IActionResult Profile() => View();

    [HttpGet]
    public async Task<IActionResult> ProfileData(int? id)
    {
        // This endpoint currently supports the signed-in member's personal center only.
        if (id.HasValue && id != CurrentId) return Forbid();
        var m = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == CurrentId && !x.IsDelete);
        if (m == null) return Unauthorized();
        return Ok(new {
            memberId=m.Id, name=m.Name, m.Email, phone=m.PhoneNumber, m.City, m.Mbti, m.Bio, m.Gender,
            birthDate=m.BirthDate?.ToString("yyyy-MM-dd"), m.Occupation, m.LineId, m.InstagramUrl, m.FacebookUrl,
            m.AvatarUrl, isSelf=true, contactVisible=true, contactBookPublic=m.ContactBookVisibility == 1,
            groupsOwnedCount=await db.TravelGroups.CountAsync(x=>x.OwnerMemberId==m.Id && !x.IsDelete),
            tripsJoinedCount=await db.GroupMembers.CountAsync(x=>x.MemberId==m.Id && !x.IsRemoved),
            skillCatalog=await db.TravelSkills.Where(x=>x.IsActive).Select(x=>new {x.SkillId,x.SkillName}).ToListAsync(),
            skillNames=await db.MemberSkills.Where(x=>x.MemberId==m.Id).Select(x=>x.Skill.SkillName).ToListAsync(),
            travelDna=await db.MemberTravelDnas.Where(x=>x.MemberId==m.Id).Select(x=>new{x.DimensionId,x.Score}).ToListAsync()
        });
    }

    public sealed class ProfileInput
    {
        [Required, StringLength(50)] public string Name { get; set; } = "";
        public DateTime? BirthDate { get; set; }
        [Range(0,2)] public byte Gender { get; set; }
        [StringLength(50)] public string? Occupation { get; set; }
        [RegularExpression("^(INTJ|INTP|ENTJ|ENTP|INFJ|INFP|ENFJ|ENFP|ISTJ|ISFJ|ESTJ|ESFJ|ISTP|ISFP|ESTP|ESFP)$")] public string? Mbti { get; set; }
        [StringLength(500)] public string? Bio { get; set; }
        [StringLength(100)] public string? City { get; set; }
        [StringLength(50)] public string? LineId { get; set; }
        [StringLength(255)] public string? InstagramUrl { get; set; }
        [StringLength(255)] public string? FacebookUrl { get; set; }
        public bool ContactBookPublic { get; set; }
        public int[]? SkillIds { get; set; }
    }
    static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    static bool SocialValid(string? value, string host) => string.IsNullOrWhiteSpace(value) ||
        (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https" &&
         (uri.Host == host || uri.Host.EndsWith("." + host, StringComparison.OrdinalIgnoreCase)));
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([FromBody] ProfileInput input)
    {
        if(!ModelState.IsValid || string.IsNullOrWhiteSpace(input.Name) || input.BirthDate > DateTime.Today ||
           !SocialValid(input.InstagramUrl,"instagram.com") || !SocialValid(input.FacebookUrl,"facebook.com"))
            return BadRequest(new{message="請確認名稱、生日與選填欄位格式；社群連結需使用正確網站的 HTTPS 網址。"});
        var m=await db.Users.SingleOrDefaultAsync(x=>x.Id==CurrentId && !x.IsDelete);
        if(m==null)return Unauthorized();
        var ids=input.SkillIds?.Distinct().ToArray();
        if(ids!=null && await db.TravelSkills.CountAsync(x=>ids.Contains(x.SkillId) && x.IsActive)!=ids.Length)
            return BadRequest(new{message="技能清單已更新，請重新整理後再試。"});
        m.Name=input.Name.Trim();m.BirthDate=input.BirthDate?.Date;m.Gender=input.Gender;
        m.Occupation=Clean(input.Occupation);m.Mbti=Clean(input.Mbti);m.Bio=Clean(input.Bio);
        m.City=Clean(input.City);m.LineId=Clean(input.LineId);m.InstagramUrl=Clean(input.InstagramUrl);
        m.FacebookUrl=Clean(input.FacebookUrl);m.ContactBookVisibility=(byte)(input.ContactBookPublic?1:0);
        if(ids!=null){
            var old=await db.MemberSkills.Where(x=>x.MemberId==m.Id).ToListAsync();
            db.MemberSkills.RemoveRange(old.Where(x=>!ids.Contains(x.SkillId)));
            foreach(var skillId in ids.Except(old.Select(x=>x.SkillId)))
                db.MemberSkills.Add(new MemberSkill{MemberId=m.Id,SkillId=skillId,CreatedAt=DateTime.Now});
        }
        await db.SaveChangesAsync();
        return Ok(new{success=true});
    }

    public sealed class DnaInput { public List<DnaScore> Scores { get; set; } = new(); }
    public sealed class DnaScore { [Range(1,4)] public byte DimensionId { get; set; } [Range(0,100)] public byte Score { get; set; } }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTravelDna([FromBody] DnaInput input)
    {
        if(!ModelState.IsValid || input.Scores == null || input.Scores.Count > 4 || input.Scores.Select(x=>x.DimensionId).Distinct().Count()!=input.Scores.Count)
            return BadRequest(new{message="DNA 分數需介於 0 與 100，且每個項目只能提交一次。"});
        if(!await db.Users.AnyAsync(x=>x.Id==CurrentId && !x.IsDelete))return Unauthorized();
        var ids=input.Scores.Select(x=>x.DimensionId).ToArray();
        if(await db.TravelDnaDimensions.CountAsync(x=>ids.Contains(x.DimensionId))!=ids.Length)return BadRequest(new{message="DNA 題目尚未設定。"});
        var rows=await db.MemberTravelDnas.Where(x=>x.MemberId==CurrentId).ToListAsync();
        foreach(var score in input.Scores){
            var row=rows.SingleOrDefault(x=>x.DimensionId==score.DimensionId);
            if(row==null){row=new MemberTravelDNA{MemberId=CurrentId,DimensionId=score.DimensionId};db.MemberTravelDnas.Add(row);}
            row.Score=score.Score;row.UpdatedAt=DateTime.Now;
        }
        await db.SaveChangesAsync();return Ok(new{success=true});
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(6*1024*1024)]
    public async Task<IActionResult> UploadAvatar(IFormFile file, [FromServices] IWebHostEnvironment environment)
    {
        if(file==null || file.Length==0 || file.Length>5*1024*1024)return BadRequest(new{message="請選擇 5 MB 以內的 PNG 或 JPG 圖片。"});
        var member=await db.Users.SingleOrDefaultAsync(x=>x.Id==CurrentId && !x.IsDelete);
        if(member==null)return Unauthorized();
        await using var memory=new MemoryStream();await file.CopyToAsync(memory);var bytes=memory.ToArray();
        var png=bytes.Length>8 && bytes.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10});
        var jpg=bytes.Length>3 && bytes[0]==255 && bytes[1]==216 && bytes[2]==255;
        if(!png && !jpg)return BadRequest(new{message="僅接受 PNG 或 JPG 圖片。"});
        var dir=Path.Combine(environment.WebRootPath,"uploads","avatars");Directory.CreateDirectory(dir);
        var name=Guid.NewGuid().ToString("N")+(png?".png":".jpg");var path=Path.Combine(dir,name);
        await System.IO.File.WriteAllBytesAsync(path,bytes);
        member.AvatarUrl="/uploads/avatars/"+name;
        try{await db.SaveChangesAsync();}catch{System.IO.File.Delete(path);throw;}
        return Ok(new{avatarUrl=member.AvatarUrl});
    }
}

