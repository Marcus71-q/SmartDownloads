using System.IO;
namespace SmartDownloads.Services;
public sealed record SafeFileMetadata(string Name, string Extension, long SizeBytes, bool IsDirectory, IReadOnlyList<string>? ArchiveEntryNames, string? WindowsDescription, string? ProductName, string? Publisher);
public static class FileMetadataCollector
{
    public static SafeFileMetadata Collect(FileSystemInfo item)
    {
        if (item is DirectoryInfo directory) return new(Limit(directory.Name, 260), "", 0, true, null, null, null, null);
        var file = (FileInfo)item;
        return new(Limit(file.Name, 260), Limit(file.Extension, 24), Math.Max(0, file.Length), false, null, null, null, null);
    }
    private static string Limit(string value, int length) => value.Length <= length ? value : value[..length];
}
