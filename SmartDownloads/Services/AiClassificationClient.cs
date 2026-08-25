using SmartDownloads.Core;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace SmartDownloads.Services;
public sealed class AiClassificationClient : IDisposable
{
    private readonly AppSettings _settings; private readonly HttpClient _http; private string? _token;
    public AiClassificationClient(AppSettings settings, HttpMessageHandler? handler = null)
    { _settings = settings; _http = handler is null ? new HttpClient() : new HttpClient(handler); _http.BaseAddress = new Uri(settings.ServerUrl.TrimEnd('/') + "/"); _http.Timeout = TimeSpan.FromSeconds(75); _http.DefaultRequestHeaders.UserAgent.ParseAdd("SmartDownloads/0.2"); _token = LoadToken(); }
    public async Task<DownloadCategory?> ClassifyAsync(FileSystemInfo item, CancellationToken token)
    {
        try { if (string.IsNullOrWhiteSpace(_token)) await RegisterAsync(token); var result = await SendAsync(item, token); if (result.Status != HttpStatusCode.Unauthorized) return result.Category; _token = null; await RegisterAsync(token); return (await SendAsync(item, token)).Category; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or CryptographicException) { return null; }
    }
    private async Task RegisterAsync(CancellationToken token)
    { using var response = await _http.PostAsJsonAsync("v1/devices/register", new { installationId = _settings.InstallationId }, token); response.EnsureSuccessStatusCode(); var registration = await response.Content.ReadFromJsonAsync<RegistrationResponse>(cancellationToken: token) ?? throw new JsonException(); _token = registration.DeviceToken; SaveToken(_token); }
    private async Task<(HttpStatusCode Status, DownloadCategory? Category)> SendAsync(FileSystemInfo item, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/classify"); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token); request.Content = JsonContent.Create(FileMetadataCollector.Collect(item));
        using var response = await _http.SendAsync(request, token); if (!response.IsSuccessStatusCode) return (response.StatusCode, null);
        var result = await response.Content.ReadFromJsonAsync<ClassificationResult>(cancellationToken: token); if (result is null || result.Confidence < .55) return (response.StatusCode, DownloadCategory.NeedsReview);
        var category = result.Category switch { "projects_and_code" => DownloadCategory.ProjectsAndCode, "games_and_mods" => DownloadCategory.GamesAndMods, "apps_and_installers" => DownloadCategory.AppsAndInstallers, "images" => DownloadCategory.Images, "videos" => DownloadCategory.Videos, "music" => DownloadCategory.Music, "models_3d" => DownloadCategory.Models3D, "documents_and_data" => DownloadCategory.DocumentsAndData, "archives" => DownloadCategory.Archives, "needs_review" => DownloadCategory.NeedsReview, _ => (DownloadCategory?)null }; return (response.StatusCode, category);
    }
    private static string? LoadToken() { try { return File.Exists(AppSettings.TokenPath) ? Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(AppSettings.TokenPath), null, DataProtectionScope.CurrentUser)) : null; } catch { return null; } }
    private static void SaveToken(string token) { Directory.CreateDirectory(Path.GetDirectoryName(AppSettings.TokenPath)!); File.WriteAllBytes(AppSettings.TokenPath, ProtectedData.Protect(Encoding.UTF8.GetBytes(token), null, DataProtectionScope.CurrentUser)); }
    public void Dispose() => _http.Dispose();
    private sealed record RegistrationResponse(string DeviceToken, DateTimeOffset ExpiresAt); private sealed record ClassificationResult(string Category, double Confidence, string Reason, string Source);
}
