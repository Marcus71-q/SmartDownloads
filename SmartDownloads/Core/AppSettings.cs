using System.IO;
using System.Text.Json;
namespace SmartDownloads.Core;
public sealed record AppSettings
{
    public string Language { get; init; } = "en";
    public bool SetupCompleted { get; init; }
    public bool MetadataConsent { get; init; }
    public bool OrganizeExistingFiles { get; init; }
    public Guid InstallationId { get; init; } = Guid.NewGuid();
    public string ServerUrl { get; init; } = BuildConfiguration.ServerUrl;
    public AppSettings() { }
    public AppSettings(string language, bool setupCompleted, bool metadataConsent = false, bool organizeExistingFiles = false)
    { Language = language; SetupCompleted = setupCompleted; MetadataConsent = metadataConsent; OrganizeExistingFiles = organizeExistingFiles; }
    private static string DirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartDownloads");
    public static string FilePath => Path.Combine(DirectoryPath, "settings.json");
    public static string TokenPath => Path.Combine(DirectoryPath, "device-token.bin");
    public static AppSettings? Load()
    {
        try { if (!File.Exists(FilePath)) return null; var value = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), JsonOptions); var url = Environment.GetEnvironmentVariable("SMARTDOWNLOADS_API_URL"); return value is null || string.IsNullOrWhiteSpace(url) ? value : value with { ServerUrl = url }; }
        catch { return null; }
    }
    public void Save() { Directory.CreateDirectory(DirectoryPath); File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions)); }
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
}
