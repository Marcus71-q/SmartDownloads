using SmartDownloads.Server.Models;
using System.Text;
using System.Text.Json;

namespace SmartDownloads.Server.Services;

public sealed class GeminiClassifier(HttpClient client, ILogger<GeminiClassifier> logger)
{
    private static readonly SemaphoreSlim Concurrency = new(8, 8);
    private static readonly string[] Categories = ["projects_and_code", "games_and_mods", "apps_and_installers", "images", "videos", "music", "models_3d", "documents_and_data", "archives", "needs_review"];

    public async Task<ClassificationResponse> ClassifyAsync(ClassificationRequest request, Guid deviceId, CancellationToken cancellationToken)
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ServiceUnavailableException("GEMINI_API_KEY is not configured on the server.");
        var model = Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? "gemini-3.6-flash";
        var prompt = "Classify this downloaded item using metadata only. Never claim it was opened or executed. Prefer needs_review when evidence is weak. Return exactly one JSON object with category, confidence (0-1), and a short reason. Category must be one of: " + string.Join(", ", Categories) + ". Metadata: " + JsonSerializer.Serialize(request);
        var payload = new
        {
            contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
            generationConfig = new
            {
                temperature = 0.1,
                maxOutputTokens = 100,
                thinkingConfig = new { thinkingLevel = "minimal" }
            }
        };

        await Concurrency.WaitAsync(cancellationToken);
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, $"models/{Uri.EscapeDataString(model)}:generateContent");
            message.Headers.Add("x-goog-api-key", apiKey);
            message.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(message, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Gemini request failed with status {StatusCode}", (int)response.StatusCode);
                throw new ServiceUnavailableException("The Gemini classification provider is temporarily unavailable.");
            }
            using var document = JsonDocument.Parse(body);
            var text = document.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
            var cleanText = (text ?? "").Trim().Replace("```json", "", StringComparison.OrdinalIgnoreCase).Replace("```", "").Trim();
            var parsed = JsonSerializer.Deserialize<ModelResult>(cleanText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (parsed is null || !Categories.Contains(parsed.Category)) throw new ServiceUnavailableException("The Gemini provider returned an invalid result.");
            var reason = parsed.Reason ?? "Classified from limited metadata.";
            return new ClassificationResponse(parsed.Category, Math.Clamp(parsed.Confidence, 0, 1), reason[..Math.Min(reason.Length, 180)], "gemini");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid JSON from Gemini provider");
            throw new ServiceUnavailableException("The Gemini provider returned an invalid result.");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Gemini request timed out");
            throw new ServiceUnavailableException("The Gemini classification provider timed out.");
        }
        finally { Concurrency.Release(); }
    }

    private sealed record ModelResult(string Category, double Confidence, string? Reason);
}
