using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using LazyTravel.Shared.Models.EfModels;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace LazyTravel.Security;

/// <summary>Provides voluntary RFC 6238 TOTP setup. It deliberately does not block normal sign-in.</summary>
public sealed class AuthenticatorSetupService
{
    private const string Provider = "LazyTravel.AccountSecurity";
    private const string SecretName = "AuthenticatorSecret";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDataProtector _protector;
    private readonly ConcurrentDictionary<string, PendingSetup> _pending = new();

    public AuthenticatorSetupService(IServiceScopeFactory scopeFactory, IDataProtectionProvider protection)
    {
        _scopeFactory = scopeFactory;
        _protector = protection.CreateProtector("LazyTravel.Authenticator.v1");
    }

    public async Task<AuthenticatorStatus> GetStatusAsync(int memberId)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LazyTravelDBContext>();
        var enabled = await db.Users.AsNoTracking().AnyAsync(member => member.Id == memberId && member.TwoFactorEnabled && !member.IsDelete && member.Status != 2);
        return new AuthenticatorStatus(enabled);
    }

    public async Task<SetupStartResult> StartAsync(int memberId)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LazyTravelDBContext>();
        var member = await db.Users.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == memberId && !candidate.IsDelete && candidate.Status != 2);
        if (member is null) return SetupStartResult.Failed("找不到可設定的會員帳號。" );
        if (member.TwoFactorEnabled) return SetupStartResult.Failed("此帳號已啟用登入雙重驗證。" );

        var secret = ToBase32(RandomNumberGenerator.GetBytes(20));
        var label = $"LazyTravel:{member.Email ?? member.Name}";
        var uri = $"otpauth://totp/{Uri.EscapeDataString(label)}?secret={secret}&issuer=LazyTravel&algorithm=SHA1&digits=6&period=30";
        var setupId = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        _pending[setupId] = new PendingSetup(memberId, secret, DateTimeOffset.UtcNow.AddMinutes(10));

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(8);
        return SetupStartResult.Ok(setupId, $"data:image/png;base64,{Convert.ToBase64String(png)}", secret);
    }

    public async Task<SetupConfirmResult> ConfirmAsync(int memberId, string setupId, string code)
    {
        if (!_pending.TryGetValue(setupId, out var pending) || pending.MemberId != memberId || pending.ExpiresAt < DateTimeOffset.UtcNow)
            return SetupConfirmResult.Failed("這組 QR Code 已逾時，請重新開始設定。" );
        if (pending.Attempts++ >= 5 || !IsValidCode(pending.Secret, code))
            return SetupConfirmResult.Failed("驗證碼不正確。請確認驗證器 App 的時間後再試。" );

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LazyTravelDBContext>();
        var member = await db.Users.SingleOrDefaultAsync(candidate => candidate.Id == memberId && !candidate.IsDelete && candidate.Status != 2);
        if (member is null) return SetupConfirmResult.Failed("找不到可設定的會員帳號。" );

        var protectedSecret = _protector.Protect(pending.Secret);
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[MemberTokens] WHERE [UserId] = {memberId} AND [LoginProvider] = {Provider} AND [Name] = {SecretName}");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [dbo].[MemberTokens] ([UserId], [LoginProvider], [Name], [Value], [ExpiresAt]) VALUES ({memberId}, {Provider}, {SecretName}, {protectedSecret}, {DateTime.UtcNow.AddYears(10)})");
        member.TwoFactorEnabled = true;
        member.SecurityStamp = Guid.NewGuid().ToString();
        member.ConcurrencyStamp = Guid.NewGuid().ToString();
        await db.SaveChangesAsync();
        _pending.TryRemove(setupId, out _);
        return SetupConfirmResult.Ok("登入雙重驗證已啟用。" );
    }

    private static bool IsValidCode(string secretText, string code)
    {
        if (code.Length != 6 || !code.All(char.IsAsciiDigit)) return false;
        var secret = DecodeBase32(secretText);
        var step = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        return Enumerable.Range(-1, 3).Select(offset => CalculateTotp(secret, step + offset))
            .Any(candidate => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(candidate), Encoding.UTF8.GetBytes(code)));
    }

    private static string CalculateTotp(byte[] secret, long timestep)
    {
        var counter = BitConverter.GetBytes(timestep); if (BitConverter.IsLittleEndian) Array.Reverse(counter);
        using var hmac = new HMACSHA1(secret); var hash = hmac.ComputeHash(counter); var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (binary % 1_000_000).ToString("D6");
    }

    private static string ToBase32(byte[] bytes)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"; var value = 0; var bits = 0; var result = new StringBuilder();
        foreach (var b in bytes) { value = (value << 8) | b; bits += 8; while (bits >= 5) { result.Append(alphabet[(value >> (bits - 5)) & 31]); bits -= 5; } }
        if (bits > 0) result.Append(alphabet[(value << (5 - bits)) & 31]); return result.ToString();
    }

    private static byte[] DecodeBase32(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"; var buffer = 0; var bits = 0; var bytes = new List<byte>();
        foreach (var c in value) { var index = alphabet.IndexOf(c); if (index < 0) throw new CryptographicException(); buffer = (buffer << 5) | index; bits += 5; if (bits >= 8) { bytes.Add((byte)(buffer >> (bits - 8))); bits -= 8; } }
        return bytes.ToArray();
    }

    private sealed class PendingSetup(int memberId, string secret, DateTimeOffset expiresAt)
    {
        public int MemberId { get; } = memberId;
        public string Secret { get; } = secret;
        public DateTimeOffset ExpiresAt { get; } = expiresAt;
        public int Attempts { get; set; }
    }
}

public sealed record AuthenticatorStatus(bool Enabled);
public sealed record SetupStartResult(bool Success, string? SetupId, string? QrCodeDataUri, string? ManualKey, string? Message)
{
    public static SetupStartResult Failed(string message) => new(false, null, null, null, message);
    public static SetupStartResult Ok(string id, string qr, string key) => new(true, id, qr, key, null);
}
public sealed record SetupConfirmResult(bool Success, string Message)
{
    public static SetupConfirmResult Failed(string message) => new(false, message);
    public static SetupConfirmResult Ok(string message) => new(true, message);
}
