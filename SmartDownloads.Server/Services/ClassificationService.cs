using Microsoft.Extensions.Caching.Memory;
using SmartDownloads.Server.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace SmartDownloads.Server.Services;
public sealed class ClassificationService(IMemoryCache cache, GeminiClassifier gemini)
{
    public async Task<ClassificationResponse> ClassifyAsync(ClassificationRequest request, Guid deviceId, CancellationToken cancellationToken)
    {
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request))));
        if (cache.TryGetValue<ClassificationResponse>(key, out var cached) && cached is not null) return cached with { Source = "cache" };
        var result = await gemini.ClassifyAsync(request, deviceId, cancellationToken);
        cache.Set(key, result, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7), Size = 1 }); return result;
    }
}
