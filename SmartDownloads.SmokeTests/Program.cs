using SmartDownloads.Services;
using System.IO.Compression;

var root = Path.Combine(Path.GetTempPath(), "SmartDownloads-SmokeTests", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    var organizer = new DownloadOrganizer("en", root);
    var image = Path.Combine(root, "holiday-photo.jpg");
    var partial = Path.Combine(root, "large-video.mp4.crdownload");
    File.WriteAllText(image, "image-test");
    File.WriteAllText(partial, "unfinished");
    File.SetLastWriteTimeUtc(image, DateTime.UtcNow.AddMinutes(-2));
    File.SetLastWriteTimeUtc(partial, DateTime.UtcNow.AddMinutes(-2));
    var zipPath = Path.Combine(root, "sample.zip");
    using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
    {
        var entry = archive.CreateEntry("mods/loader.dll");
        using var writer = new StreamWriter(entry.Open()); writer.Write("secret-content-must-not-be-collected");
    }
    var metadata = FileMetadataCollector.Collect(new FileInfo(zipPath));
    Assert(metadata.ArchiveEntryNames?.Single() == "mods/loader.dll", "ZIP metadata names were not collected safely.");
    Assert(!metadata.ToString().Contains("secret-content", StringComparison.Ordinal), "ZIP content leaked into metadata.");

    await organizer.ScanNowAsync();
    await organizer.ScanNowAsync();
    Assert(File.Exists(Path.Combine(root, "04_Images", "holiday-photo.jpg")), "Image was not classified correctly.");
    Assert(File.Exists(partial), "Partial download must not be moved.");

    var duplicate = Path.Combine(root, "holiday-photo.jpg");
    File.WriteAllText(duplicate, "second-image");
    File.SetLastWriteTimeUtc(duplicate, DateTime.UtcNow.AddMinutes(-2));
    await organizer.ScanNowAsync();
    await organizer.ScanNowAsync();
    Assert(File.Exists(Path.Combine(root, "04_Images", "holiday-photo (2).jpg")), "Duplicate name was overwritten instead of renamed.");
    organizer.Dispose();
    Console.WriteLine("PASS: classification, partial-download protection, duplicate-name protection, metadata minimization");
}
finally
{
    var safeRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "SmartDownloads-SmokeTests"));
    var resolved = Path.GetFullPath(root);
    if (resolved.StartsWith(safeRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && Directory.Exists(resolved)) Directory.Delete(resolved, true);
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
