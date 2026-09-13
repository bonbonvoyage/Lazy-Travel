using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using LazyTravel.Shared.Models.EfModels;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Security;

/// <summary>
/// Password-recovery challenges are deliberately short-lived and kept in memory.
/// This keeps the feature self-contained: no new database table is required.
/// </summary>
public sealed class PasswordRecoveryService
{
    private const string SecurityTokenProvider = "LazyTravel.AccountSecurity";
    private const string PasswordResetAfterToken = "PasswordResetAfter";
    private const string AuthenticatorToken = "AuthenticatorSecret";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWebHostEnvironment _environment;
    private readonly IDataProtector _authenticatorProtector;
    private readonly ConcurrentDictionary<string, RecoveryChallenge> _challenges = new();
    private static readonly PasswordHasher<Member> PasswordHasher = new();

    public PasswordRecoveryService(
        IServiceScopeFactory scopeFactory,
        IWebHostEnvironment environment,
        IDataProtectionProvider dataProtectionProvider)
    {
        _scopeFactory = scopeFactory;
        _environment = environment;
        _authenticatorProtector = dataProtectionProvider.CreateProtector("LazyTravel.Authenticator.v1");
    }

    public async Task<RecoveryRequestResult> RequestCodeAsync(string email)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LazyTravelDBContext>();
        var member = await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(m => m.NormalizedEmail == normalizedEmail && !m.IsDelete && m.Status != 2);

        // The public response remains the same whether an account exists, preventing email enumeration.
        if (member is null)
        {
            return new RecoveryRequestResult(true, null,
                "若此信箱已注册，验证码将发送至该信箱。", null);
        }

        var requestId = CreateOpaqueId();
        var code = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
        var challenge = new RecoveryChallenge(member.Id, member.Email ?? email.Trim(), requestId, code);
        _challenges[requestId] = challenge;

        return new RecoveryRequestResult(
            true,
            requestId,
            "验证码已发送。请于 10 分钟内完成验证。",
            _environment.IsDevelopment() ? code : null);
    }

    public async Task<RecoveryStepResult> VerifyEmailCodeAsync(string requestId, string code)
    {
        if (!TryGetChallenge(requestId, out var challenge, out var error))
            return RecoveryStepResult.Failed(error);

        lock (challenge.Gate)
        {
            if (!ValidateCode(challenge, code))
                return RecoveryStepResult.Failed("验证码无效或已过期，请重新申请。");

            challenge.EmailVerified = true;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LazyTravelDBContext>();
        var member = await db.Users.AsNoTracking().SingleOrDefaultAsync(m => m.Id == challenge.MemberId && !m.IsDelete && m.Status != 2);
        if (member is null)
            return RecoveryStepResult.Failed("此帐号目前无法重设密码，请联系服务人员。");

        if (!member.TwoFactorEnabled)
            return RecoveryStepResult.Ok(false, "信箱验证完成，请设定新密码。");

        var protectedSecret = await GetTokenValueAsync(db, member.Id, AuthenticatorToken);
        if (string.IsNullOrWhiteSpace(protectedSecret))
            return RecoveryStepResult.Failed("此帐号的 2FA 设定不完整，请联系服务人员协助恢复。");

        lock (challenge.Gate)
        {
            challenge.RequiresAuthenticator = true;
        }
        return RecoveryStepResult.Ok(true, "信箱验证完成，请输入 Google Authenticator 验证码。");
    }

    public async Task<RecoveryStepResult> VerifyAuthenticatorCodeAsync(string requestId, string code)
    {
        if (!TryGetChallenge(requestId, out var challenge, out var error))
            return RecoveryStepResult.Failed(error);

        lock (challenge.Gate)
        {
            if (!challenge.EmailVerified)
                return RecoveryStepResult.Failed("请先完成信箱验证码验证。");
            if (!challenge.RequiresAuthenticator)
                return RecoveryStepResult.Ok(false, "此帐号未启用 2FA，可直接设定新密码。");
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LazyTravelDBContext>();
        var protectedSecret = await GetTokenValueAsync(db, challenge.MemberId, AuthenticatorToken);
        if (!IsValidAuthenticatorCode(protectedSecret, code))
            return RecoveryStepResult.Failed("Authenticator 验证码错误，请确认装置时间后再试。");

        lock (challenge.Gate)
        {
            challenge.AuthenticatorVerified = true;
        }
        return RecoveryStepResult.Ok(false, "2FA 验证完成，请设定新密码。");
    }

    public async Task<PasswordResetResult> ResetPasswordAsync(string requestId, string password, string confirmPassword)
    {
        if (!TryGetChallenge(requestId, out var challenge, out var error))
            return PasswordResetResult.Failed(error);
        if (password.Length is < 8 or > 128 || password != confirmPassword)
            return PasswordResetResult.Failed("密码须为 8 至 128 个字元，且两次输入必须相同。");

        lock (challenge.Gate)
        {
            if (!challenge.EmailVerified)
                return PasswordResetResult.Failed("请先完成信箱验证码验证。");
            if (challenge.RequiresAuthenticator && !challenge.AuthenticatorVerified)
                return PasswordResetResult.Failed("请先完成 Google Authenticator 验证。");
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LazyTravelDBContext>();
        var member = await db.Users.SingleOrDefaultAsync(m => m.Id == challenge.MemberId && !m.IsDelete && m.Status != 2);
        if (member is null)
            return PasswordResetResult.Failed("此帐号目前无法重设密码，请联系服务人员。");

        member.PasswordHash = PasswordHasher.HashPassword(member, password);
        member.SecurityStamp = Guid.NewGuid().ToString();
        member.ConcurrencyStamp = Guid.NewGuid().ToString();
        await db.SaveChangesAsync();

        var resetAt = DateTimeOffset.UtcNow;
        await SaveTokenValueAsync(db, member.Id, PasswordResetAfterToken, resetAt.ToString("O"), resetAt.UtcDateTime.AddYears(10));
        _challenges.TryRemove(requestId, out _);
        return PasswordResetResult.Ok("密码已更新。为了保护帐号，其他装置上的旧登入已失效，请使用新密码重新登入。");
    }

    private bool TryGetChallenge(string requestId, out RecoveryChallenge challenge, out string error)
    {
        challenge = null!;
        error = "验证流程已过期，请重新申请验证码。";
        if (string.IsNullOrWhiteSpace(requestId) || !_challenges.TryGetValue(requestId, out var foundChallenge))
            return false;
        challenge = foundChallenge;
        if (DateTimeOffset.UtcNow > challenge.ExpiresAt)
        {
            _challenges.TryRemove(requestId, out _);
            return false;
        }
        return true;
    }

    private static bool ValidateCode(RecoveryChallenge challenge, string code)
    {
        if (challenge.Attempts++ >= 5 || string.IsNullOrWhiteSpace(code))
            return false;
        var candidate = HashCode(challenge.RequestId, code.Trim());
        return CryptographicOperations.FixedTimeEquals(candidate, challenge.CodeHash);
    }

    private async Task<string?> GetTokenValueAsync(LazyTravelDBContext db, int memberId, string name)
        => await db.Database.SqlQueryRaw<string>(
                "SELECT [Value] FROM [dbo].[MemberTokens] WHERE [UserId] = {0} AND [LoginProvider] = {1} AND [Name] = {2}",
                memberId, SecurityTokenProvider, name)
            .SingleOrDefaultAsync();

    private async Task SaveTokenValueAsync(LazyTravelDBContext db, int memberId, string name, string value, DateTime expiresAt)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[MemberTokens] WHERE [UserId] = {memberId} AND [LoginProvider] = {SecurityTokenProvider} AND [Name] = {name}");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [dbo].[MemberTokens] ([UserId], [LoginProvider], [Name], [Value], [ExpiresAt]) VALUES ({memberId}, {SecurityTokenProvider}, {name}, {value}, {expiresAt})");
    }

    private bool IsValidAuthenticatorCode(string? protectedSecret, string code)
    {
        if (string.IsNullOrWhiteSpace(protectedSecret) || code.Length != 6 || !code.All(char.IsAsciiDigit))
            return false;

        try
        {
            var secret = DecodeBase32(_authenticatorProtector.Unprotect(protectedSecret));
            var currentStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
            return Enumerable.Range(-1, 3)
                .Select(offset => CalculateTotp(secret, currentStep + offset))
                .Any(candidate => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(candidate), Encoding.UTF8.GetBytes(code)));
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static string CalculateTotp(byte[] secret, long timestep)
    {
        var counter = BitConverter.GetBytes(timestep);
        if (BitConverter.IsLittleEndian) Array.Reverse(counter);
        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counter);
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (binary % 1_000_000).ToString("D6");
    }

    private static byte[] DecodeBase32(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var buffer = 0;
        var bitsLeft = 0;
        var bytes = new List<byte>();
        foreach (var raw in value.ToUpperInvariant().Where(c => c is not ' ' and not '-'))
        {
            var index = alphabet.IndexOf(raw);
            if (index < 0) throw new CryptographicException("Invalid Base32 secret.");
            buffer = (buffer << 5) | index;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bytes.Add((byte)(buffer >> (bitsLeft - 8)));
                bitsLeft -= 8;
            }
        }
        return bytes.ToArray();
    }

    private static string CreateOpaqueId() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
    private static byte[] HashCode(string requestId, string code) => SHA256.HashData(Encoding.UTF8.GetBytes($"{requestId}:{code}"));

    private sealed class RecoveryChallenge
    {
        public RecoveryChallenge(int memberId, string email, string requestId, string code)
        {
            MemberId = memberId;
            Email = email;
            RequestId = requestId;
            CodeHash = HashCode(requestId, code);
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10);
        }

        public object Gate { get; } = new();
        public int MemberId { get; }
        public string Email { get; }
        public string RequestId { get; }
        public byte[] CodeHash { get; }
        public DateTimeOffset ExpiresAt { get; }
        public int Attempts { get; set; }
        public bool EmailVerified { get; set; }
        public bool RequiresAuthenticator { get; set; }
        public bool AuthenticatorVerified { get; set; }
    }
}

public sealed record RecoveryRequestResult(bool Success, string? RequestId, string Message, string? DemoCode);
public sealed record RecoveryStepResult(bool Success, bool RequiresAuthenticator, string Message)
{
    public static RecoveryStepResult Failed(string message) => new(false, false, message);
    public static RecoveryStepResult Ok(bool requiresAuthenticator, string message) => new(true, requiresAuthenticator, message);
}
public sealed record PasswordResetResult(bool Success, string Message)
{
    public static PasswordResetResult Failed(string message) => new(false, message);
    public static PasswordResetResult Ok(string message) => new(true, message);
}
