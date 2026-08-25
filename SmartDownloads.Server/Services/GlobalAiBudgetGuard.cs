namespace SmartDownloads.Server.Services;
public sealed class GlobalAiBudgetGuard
{
    private readonly object _gate = new(); private DateOnly _day = DateOnly.FromDateTime(DateTime.UtcNow); private int _count;
    public bool TryReserve()
    {
        lock (_gate)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow); if (today != _day) { _day = today; _count = 0; }
            var limit = int.TryParse(Environment.GetEnvironmentVariable("GLOBAL_DAILY_AI_LIMIT"), out var configured) ? Math.Clamp(configured, 100, 1_000_000) : 50_000;
            if (_count >= limit) return false; _count++; return true;
        }
    }
}
public sealed class ServiceUnavailableException(string message) : Exception(message);
