using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace SmartDownloads.Server.Services;
public sealed class DeviceTokenService
{
    private readonly byte[] _secret;
    public DeviceTokenService()
    {
        var secret = Environment.GetEnvironmentVariable("DEVICE_TOKEN_SECRET");
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32) throw new InvalidOperationException("DEVICE_TOKEN_SECRET must contain at least 32 characters.");
        _secret = Encoding.UTF8.GetBytes(secret);
    }
    public string Create(Guid installationId, TimeSpan lifetime)
    {
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new TokenPayload(installationId, DateTimeOffset.UtcNow.Add(lifetime)))));
        return payload + "." + Base64UrlEncode(Sign(payload));
    }
    public bool TryRead(string token, out Guid installationId)
    {
        installationId = Guid.Empty;
        try
        {
            var parts = token.Split('.', 2); if (parts.Length != 2) return false;
            if (!CryptographicOperations.FixedTimeEquals(Sign(parts[0]), Base64UrlDecode(parts[1]))) return false;
            var payload = JsonSerializer.Deserialize<TokenPayload>(Encoding.UTF8.GetString(Base64UrlDecode(parts[0])));
            if (payload is null || payload.ExpiresAt <= DateTimeOffset.UtcNow) return false;
            installationId = payload.InstallationId; return installationId != Guid.Empty;
        }
        catch { return false; }
    }
    private byte[] Sign(string payload) => HMACSHA256.HashData(_secret, Encoding.UTF8.GetBytes(payload));
    private static string Base64UrlEncode(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
        return Convert.FromBase64String(padded);
    }
    private sealed record TokenPayload(Guid InstallationId, DateTimeOffset ExpiresAt);
}
