using LazyTravel.Shared.Models.DTOs;
using LazyTravel.Shared.Models.EfModels;
using LazyTravel.Shared.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LazyTravel.Controllers
{
    public class AuthController : Controller
    {
        private const string MemberScheme = "MemberAuth";
        private const string ExternalScheme = "ExternalAuth";
        private static readonly HashSet<string> SupportedProviders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Google",
            "Facebook"
        };

        private readonly IMemberAuthService _memberAuthService;
        private readonly LazyTravelDBContext _db;
        private readonly IAuthenticationSchemeProvider _schemes;

        public AuthController(
            IMemberAuthService memberAuthService,
            LazyTravelDBContext db,
            IAuthenticationSchemeProvider schemes)
        {
            _memberAuthService = memberAuthService;
            _db = db;
            _schemes = schemes;
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(MemberLoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            string ua = Request.Headers.UserAgent.ToString();
            var result = _memberAuthService.Login(dto.Email, dto.Password, ip, ua);

            if (!result.Success || result.MemberData is null)
            {
                return BadRequest(result.Message);
            }

            await SignInMemberAsync(
                result.MemberData.MemberID,
                result.MemberData.Name,
                result.MemberData.Email,
                dto.RememberMe);

            return Ok(result.MemberData);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLogin(string provider, string? returnUrl = "/")
        {
            if (!SupportedProviders.Contains(provider) || await _schemes.GetSchemeAsync(provider) is null)
            {
                return RedirectToAuthError("這個第三方登入尚未完成設定，請先使用電子信箱登入。", returnUrl);
            }

            var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
            var callbackUrl = Url.Action(nameof(ExternalCallback), "Auth", new { provider, returnUrl = safeReturnUrl })!;
            return Challenge(new AuthenticationProperties { RedirectUri = callbackUrl }, provider);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalCallback(string provider, string? returnUrl = "/", string? remoteError = null)
        {
            var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
            if (!SupportedProviders.Contains(provider))
            {
                return RedirectToAuthError("不支援的登入方式。", safeReturnUrl);
            }

            if (!string.IsNullOrWhiteSpace(remoteError))
            {
                return RedirectToAuthError("第三方登入已取消，請再試一次。", safeReturnUrl);
            }

            var external = await HttpContext.AuthenticateAsync(ExternalScheme);
            if (!external.Succeeded || external.Principal is null)
            {
                return RedirectToAuthError("無法取得第三方帳號資料，請再試一次。", safeReturnUrl);
            }

            var providerKey = external.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = external.Principal.FindFirstValue(ClaimTypes.Email)
                ?? external.Principal.FindFirstValue("email");
            var displayName = external.Principal.FindFirstValue(ClaimTypes.Name)
                ?? external.Principal.FindFirstValue("name")
                ?? email?.Split('@')[0];

            if (string.IsNullOrWhiteSpace(providerKey) || string.IsNullOrWhiteSpace(email))
            {
                await HttpContext.SignOutAsync(ExternalScheme);
                return RedirectToAuthError("第三方帳號沒有提供電子信箱，無法建立會員資料。", safeReturnUrl);
            }

            email = email.Trim();
            var normalizedEmail = email.ToUpperInvariant();

            await using var transaction = await _db.Database.BeginTransactionAsync();
            await _db.Database.ExecuteSqlRawAsync(
                "DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource=N'LazyTravel.ExternalLogin', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @result < 0 THROW 50002, 'External login busy', 1;");

            var login = await _db.Set<IdentityUserLogin<int>>()
                .SingleOrDefaultAsync(x => x.LoginProvider == provider && x.ProviderKey == providerKey);

            Member? member;
            if (login is not null)
            {
                member = await _db.Users.SingleOrDefaultAsync(x => x.Id == login.UserId);
            }
            else
            {
                member = await _db.Users.FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail);
                if (member is null)
                {
                    var name = string.IsNullOrWhiteSpace(displayName) ? "LazyTravel 旅人" : displayName.Trim();
                    if (name.Length > 50) name = name[..50];

                    member = new Member
                    {
                        Name = name,
                        Email = email,
                        NormalizedEmail = normalizedEmail,
                        EmailConfirmed = true,
                        UserName = email,
                        NormalizedUserName = normalizedEmail,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        ConcurrencyStamp = Guid.NewGuid().ToString(),
                        Status = 1,
                        CreatedAt = DateTime.Now,
                        LockoutEnabled = true,
                        Gender = 0
                    };
                    _db.Users.Add(member);
                    await _db.SaveChangesAsync();
                }

                _db.Set<IdentityUserLogin<int>>().Add(new IdentityUserLogin<int>
                {
                    LoginProvider = provider,
                    ProviderKey = providerKey,
                    ProviderDisplayName = provider,
                    UserId = member.Id
                });
            }

            if (member is null || member.IsDelete || member.Status == 2)
            {
                await transaction.RollbackAsync();
                await HttpContext.SignOutAsync(ExternalScheme);
                return RedirectToAuthError("此會員帳號目前無法登入，請聯繫客服。", safeReturnUrl);
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            member.LastLoginAt = DateTime.Now;
            member.LastLoginIp = ip;
            _db.LoginHistories.Add(new LoginHistory
            {
                MemberId = member.Id,
                LoginIp = ip,
                IsSuccess = true,
                UserAgent = Request.Headers.UserAgent.ToString(),
                AttemptedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            await HttpContext.SignOutAsync(ExternalScheme);
            await SignInMemberAsync(member.Id, member.Name, member.Email ?? email, true);
            return LocalRedirect(safeReturnUrl);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = MemberScheme)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(MemberScheme);
            await HttpContext.SignOutAsync(ExternalScheme);
            return Ok(new { success = true, redirectUrl = "/" });
        }

        private async Task SignInMemberAsync(int id, string name, string email, bool persistent)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, id.ToString()),
                new(ClaimTypes.Name, name),
                new(ClaimTypes.Email, email)
            };
            var identity = new ClaimsIdentity(claims, MemberScheme);
            var properties = new AuthenticationProperties { IsPersistent = persistent };
            await HttpContext.SignInAsync(MemberScheme, new ClaimsPrincipal(identity), properties);
        }

        private IActionResult RedirectToAuthError(string message, string? returnUrl)
        {
            var destination = Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";
            var separator = destination.Contains('?') ? '&' : '?';
            return LocalRedirect($"{destination}{separator}authError={Uri.EscapeDataString(message)}");
        }
    }
}
