using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using LazyTravel.Shared.Models.EfModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Controllers;

public class RegistrationController(LazyTravelDBContext db) : Controller
{
    public sealed class RegistrationInput
    {
        [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
        [Required, StringLength(50)] public string Name { get; set; } = "";
        [Required, StringLength(72, MinimumLength = 8)] public string Password { get; set; } = "";
        [Required, Compare(nameof(Password))] public string ConfirmPassword { get; set; } = "";
    }

    [HttpPost("/Auth/Register"), AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegistrationInput input)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(input.Name) || Encoding.UTF8.GetByteCount(input.Password) > 72)
            return BadRequest(new { message = "請填寫有效的電子信箱、個人名稱，以及 8 個字元以上、72 位元組以內且確認一致的密碼。" });
        if (User.Identity?.IsAuthenticated == true)
            return Conflict(new { message = "你已登入，請先登出再建立其他帳號。" });
        var email = input.Email.Trim();
        var normalized = email.ToUpperInvariant();
        var hash = BCrypt.Net.BCrypt.HashPassword(input.Password);
        // The supplied schema has no unique email index. Serialize registrations across app instances.
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource=N'LazyTravel.MemberRegistration', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @result < 0 THROW 50001, 'Registration busy', 1;");
        if (await db.Users.AnyAsync(m => m.NormalizedEmail == normalized || m.Email!.ToUpper() == normalized))
            return Conflict(new { message = "這個電子信箱已經註冊，請直接登入。" });
        var member = new Member {
            Name = input.Name.Trim(), Email = email, NormalizedEmail = normalized,
            UserName = email, NormalizedUserName = normalized, PasswordHash = hash,
            SecurityStamp = Guid.NewGuid().ToString(), ConcurrencyStamp = Guid.NewGuid().ToString(),
            Status = 1, CreatedAt = DateTime.Now, LockoutEnabled = true, Gender = 0
        };
        db.Users.Add(member);
        await db.SaveChangesAsync();
        await tx.CommitAsync(); // Account exists even if the user leaves the following screen.
        var identity = new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, member.Id.ToString()),
            new Claim(ClaimTypes.Name, member.Name), new Claim(ClaimTypes.Email, email)
        }, "MemberAuth");
        await HttpContext.SignInAsync("MemberAuth", new ClaimsPrincipal(identity));
        return Ok(new { success = true, memberId = member.Id, redirectUrl = "/Members/Profile" });
    }
}

