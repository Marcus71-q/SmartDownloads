using System.Collections.Concurrent;
namespace SmartDownloads.Server.Services;
public sealed class DailyQuotaService
{
    private readonly ConcurrentDictionary<(Guid Device, DateOnly Day), int> _usage = new();
    public bool TryConsume(Guid deviceId, int limit, out int remaining)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow); var used = _usage.AddOrUpdate((deviceId, today), 1, (_, value) => value + 1); remaining = Math.Max(0, limit - used);
        if (_usage.Count > 100_000) foreach (var key in _usage.Keys.Where(x => x.Day < today)) _usage.TryRemove(key, out _);
        return used <= limit;
    }
}
