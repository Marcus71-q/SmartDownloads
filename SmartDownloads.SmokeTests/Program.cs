using SmartDownloads.Services;

var root = Path.Combine(Path.GetTempPath(), "SmartDownloads-SmokeTests", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    var image = Path.Combine(root, "holiday-photo.jpg");
    var partial = Path.Combine(root, "large-video.mp4.crdownload");
    File.WriteAllText(image, "image-test");
    File.WriteAllText(partial, "unfinished");
    File.SetLastWriteTimeUtc(image, DateTime.UtcNow.AddMinutes(-2));
    File.SetLastWriteTimeUtc(partial, DateTime.UtcNow.AddMinutes(-2));
    var zipPath = Path.Combine(root, "sample.zip");
    File.WriteAllText(zipPath, "not-opened-by-metadata-collector");
    var metadata = FileMetadataCollector.Collect(new FileInfo(zipPath));
    Assert(metadata.ArchiveEntryNames is null, "Archive entries must not be opened or collected.");
    Assert(metadata.WindowsDescription is null && metadata.Publisher is null, "Executable internals must not be inspected.");

    var organizer = new DownloadOrganizer("en", root);
    await organizer.ScanNowAsync();
    await organizer.ScanNowAsync();
    Assert(File.Exists(Path.Combine(root, "04_Images", "holiday-photo.jpg")), "Image was not classified correctly.");
    Assert(File.Exists(partial), "Partial download must not be moved.");
    organizer.Dispose();

    var duplicate = Path.Combine(root, "holiday-photo.jpg");
    File.WriteAllText(duplicate, "second-image");
    File.SetLastWriteTimeUtc(duplicate, DateTime.UtcNow.AddMinutes(-2));
    using var duplicateOrganizer = new DownloadOrganizer("en", root);
    await duplicateOrganizer.ScanNowAsync();
    await duplicateOrganizer.ScanNowAsync();
    Assert(File.Exists(Path.Combine(root, "04_Images", "holiday-photo (2).jpg")), "Duplicate name was overwritten instead of renamed.");
    Assert(duplicateOrganizer.TryUndoLastMove(), "Last move could not be undone.");
    Assert(File.Exists(duplicate), "Undo did not restore the original path.");

    var safeDefaultRoot = Path.Combine(root, "safe-default");
    Directory.CreateDirectory(safeDefaultRoot);
    var oldFile = Path.Combine(safeDefaultRoot, "existing.jpg");
    File.WriteAllText(oldFile, "existing");
    File.SetLastWriteTimeUtc(oldFile, DateTime.UtcNow.AddMinutes(-2));
    using var safeDefault = new DownloadOrganizer(new SmartDownloads.Core.AppSettings("en", true, true, false), safeDefaultRoot, false);
    await safeDefault.ScanNowAsync();
    await safeDefault.ScanNowAsync();
    Assert(File.Exists(oldFile), "Existing files must remain untouched by default.");
    Console.WriteLine("PASS: safe-new-file mode, classification, partial-download protection, duplicate protection, undo, metadata minimization");
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
