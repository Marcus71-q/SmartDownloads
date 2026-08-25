using SmartDownloads.Core;
using System.IO;

namespace SmartDownloads.Services;

public sealed class LocalFileClassifier
{
    public DownloadCategory Classify(FileSystemInfo item)
    {
        var name = item.Name.ToLowerInvariant();
        if (item is DirectoryInfo)
        {
            if (Matches(name, "source", "project", "proje", "github", "code", "api", "bot", "web", "site", "nyxbeat", "vercel", "discord")) return DownloadCategory.ProjectsAndCode;
            if (Matches(name, "game", "oyun", "mod", "bepinex", "reshade", "rdr2", "steam", "itch", "deadly", "among", "atlas")) return DownloadCategory.GamesAndMods;
            if (Matches(name, "setup", "installer", "portable", "tool", "program", "utility", "driver")) return DownloadCategory.AppsAndInstallers;
            return DownloadCategory.NeedsReview;
        }

        var extension = item.Extension.ToLowerInvariant();
        if (In(extension, ".py", ".js", ".ts", ".tsx", ".jsx", ".cs", ".cpp", ".c", ".h", ".java", ".kt", ".go", ".rs", ".php", ".rb", ".swift", ".sql", ".sh", ".bat", ".ps1", ".html", ".css", ".vue", ".svelte", ".ipynb")) return DownloadCategory.ProjectsAndCode;
        if (In(extension, ".dll", ".apk", ".xapk") || Matches(name, "mod", "bepinex", "reshade", "rdr2", "steam", "itch", "save file", "malummenu", "deadlyfight", "atlasplaybook")) return DownloadCategory.GamesAndMods;
        if (In(extension, ".exe", ".msi", ".msix", ".appx", ".appxbundle") || Matches(name, "setup", "installer", "portable")) return DownloadCategory.AppsAndInstallers;
        if (In(extension, ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg", ".ico", ".tiff", ".heic", ".avif")) return DownloadCategory.Images;
        if (In(extension, ".mp4", ".mkv", ".avi", ".mov", ".webm", ".wmv", ".m4v", ".flv")) return DownloadCategory.Videos;
        if (In(extension, ".mp3", ".wav", ".flac", ".m4a", ".ogg", ".aac", ".wma", ".opus")) return DownloadCategory.Music;
        if (In(extension, ".glb", ".gltf", ".fbx", ".obj", ".blend", ".stl", ".3ds")) return DownloadCategory.Models3D;
        if (In(extension, ".pdf", ".doc", ".docx", ".odt", ".txt", ".md", ".rtf", ".json", ".xml", ".yaml", ".yml", ".csv", ".xls", ".xlsx", ".ppt", ".pptx", ".epub", ".har")) return DownloadCategory.DocumentsAndData;
        if (In(extension, ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".iso")) return DownloadCategory.Archives;
        return DownloadCategory.NeedsReview;
    }

    private static bool In(string value, params string[] candidates) => candidates.Contains(value, StringComparer.OrdinalIgnoreCase);
    private static bool Matches(string value, params string[] terms) => terms.Any(value.Contains);
}
