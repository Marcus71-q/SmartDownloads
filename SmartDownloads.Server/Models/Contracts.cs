namespace SmartDownloads.Server.Models;
public sealed record DeviceRegistration(string InstallationId);
public sealed record DeviceRegistrationResponse(string DeviceToken, DateTimeOffset ExpiresAt);
public sealed record ApiError(string Code, string Message);
public sealed record ClassificationRequest(string Name, string Extension, long SizeBytes, bool IsDirectory, IReadOnlyList<string>? ArchiveEntryNames, string? WindowsDescription, string? ProductName, string? Publisher)
{
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 260) return "Name is required and must be at most 260 characters.";
        if (Extension is null || Extension.Length > 24) return "Extension must be present and at most 24 characters.";
        if (SizeBytes < 0 || SizeBytes > 5L * 1024 * 1024 * 1024 * 1024) return "SizeBytes is outside the accepted range.";
        if (ArchiveEntryNames is { Count: > 100 }) return "At most 100 archive entry names are accepted.";
        if (ArchiveEntryNames?.Any(x => x is null || x.Length > 240) == true) return "Archive entry names must be non-null and at most 240 characters.";
        if (WindowsDescription?.Length > 300 || ProductName?.Length > 200 || Publisher?.Length > 200) return "Metadata field is too long.";
        return null;
    }
}
public sealed record ClassificationResponse(string Category, double Confidence, string Reason, string Source);
