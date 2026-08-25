using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;
namespace SmartDownloads.Server.Services;
public sealed class DeviceTokenService(IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector = provider.CreateProtector("device-access-v1");
    public string Create(Guid installationId, TimeSpan lifetime) => _protector.Protect(JsonSerializer.Serialize(new TokenPayload(installationId, DateTimeOffset.UtcNow.Add(lifetime))));
    public bool TryRead(string token, out Guid installationId)
    {
        installationId = Guid.Empty;
        try { var payload = JsonSerializer.Deserialize<TokenPayload>(_protector.Unprotect(token)); if (payload is null || payload.ExpiresAt <= DateTimeOffset.UtcNow) return false; installationId = payload.InstallationId; return installationId != Guid.Empty; }
        catch { return false; }
    }
    private sealed record TokenPayload(Guid InstallationId, DateTimeOffset ExpiresAt);
}
