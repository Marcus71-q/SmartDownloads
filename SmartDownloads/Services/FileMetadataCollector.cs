using System.Diagnostics;
using System.IO;
using System.IO.Compression;
namespace SmartDownloads.Services;
public sealed record SafeFileMetadata(string Name, string Extension, long SizeBytes, bool IsDirectory, IReadOnlyList<string>? ArchiveEntryNames, string? WindowsDescription, string? ProductName, string? Publisher);
public static class FileMetadataCollector
{
    public static SafeFileMetadata Collect(FileSystemInfo item)
    {
        if (item is DirectoryInfo directory) return new(directory.Name, "", 0, true, directory.EnumerateFileSystemInfos().Take(50).Select(x => Limit(x.Name, 240)).ToArray(), null, null, null);
        var file = (FileInfo)item; IReadOnlyList<string>? entries = null;
        if (file.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)) try { using var archive = ZipFile.OpenRead(file.FullName); entries = archive.Entries.Take(100).Select(x => Limit(x.FullName, 240)).ToArray(); } catch { }
        string? description = null, product = null, publisher = null;
        if (file.Extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) || file.Extension.Equals(".dll", StringComparison.OrdinalIgnoreCase)) try { var info = FileVersionInfo.GetVersionInfo(file.FullName); description = LimitNullable(info.FileDescription, 300); product = LimitNullable(info.ProductName, 200); publisher = LimitNullable(info.CompanyName, 200); } catch { }
        return new(Limit(file.Name, 260), Limit(file.Extension, 24), Math.Max(0, file.Length), false, entries, description, product, publisher);
    }
    private static string Limit(string value, int length) => value.Length <= length ? value : value[..length];
    private static string? LimitNullable(string? value, int length) => string.IsNullOrWhiteSpace(value) ? null : Limit(value, length);
}
