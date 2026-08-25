using System.Collections.Concurrent;
using StackExchange.Redis;

namespace SmartDownloads.Server.Services;

public sealed class QuotaService : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, int> _memory = new();
    private readonly Lazy<Task<ConnectionMultiplexer>>? _redis;

    public QuotaService()
    {
        var url = Environment.GetEnvironmentVariable("REDIS_URL");
        if (!string.IsNullOrWhiteSpace(url))
            _redis = new Lazy<Task<ConnectionMultiplexer>>(() => ConnectionMultiplexer.ConnectAsync(CreateRedisOptions(url)));
    }

    public Task<(bool Allowed, int Remaining)> TryConsumeDeviceAsync(Guid deviceId)
    {
        var day = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var deviceLimit = ReadLimit("DEVICE_DAILY_AI_LIMIT", 500, 10, 10_000);
        return IncrementAsync($"quota:device:{day}:{deviceId:N}", deviceLimit);
    }

    public Task<(bool Allowed, int Remaining)> TryReserveGlobalAiAsync()
    {
        var day = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var globalLimit = ReadLimit("GLOBAL_DAILY_AI_LIMIT", 1_000, 100, 100_000);
        return IncrementAsync($"quota:global:{day}", globalLimit);
    }

    private async Task<(bool Allowed, int Remaining)> IncrementAsync(string key, int limit)
    {
        if (_redis is null)
        {
            var used = _memory.AddOrUpdate(key, 1, (_, value) => value + 1);
            return (used <= limit, Math.Max(0, limit - used));
        }

        try
        {
            var database = (await _redis.Value).GetDatabase();
            var used = await database.StringIncrementAsync(key);
            if (used == 1) await database.KeyExpireAsync(key, TimeSpan.FromDays(2));
            return (used <= limit, Math.Max(0, limit - (int)used));
        }
        catch (RedisException)
        {
            throw new ServiceUnavailableException("Quota storage is temporarily unavailable.");
        }
    }

    private static int ReadLimit(string name, int fallback, int minimum, int maximum) =>
        int.TryParse(Environment.GetEnvironmentVariable(name), out var configured) ? Math.Clamp(configured, minimum, maximum) : fallback;

    private static ConfigurationOptions CreateRedisOptions(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "redis" && uri.Scheme != "rediss"))
        {
            var parsed = ConfigurationOptions.Parse(value);
            parsed.AbortOnConnectFail = false;
            return parsed;
        }

        var options = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            ConnectRetry = 3,
            ConnectTimeout = 5_000,
            Ssl = uri.Scheme == "rediss"
        };
        options.EndPoints.Add(uri.Host, uri.IsDefaultPort ? 6379 : uri.Port);

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var credentials = uri.UserInfo.Split(':', 2);
            if (credentials.Length == 2)
            {
                options.User = Uri.UnescapeDataString(credentials[0]);
                options.Password = Uri.UnescapeDataString(credentials[1]);
            }
            else
            {
                options.Password = Uri.UnescapeDataString(credentials[0]);
            }
        }

        return options;
    }

    public async ValueTask DisposeAsync()
    {
        if (_redis?.IsValueCreated != true) return;
        try { (await _redis.Value).Dispose(); }
        catch (RedisException) { }
    }
}

public sealed class ServiceUnavailableException(string message) : Exception(message);
