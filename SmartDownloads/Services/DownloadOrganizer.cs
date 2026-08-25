using SmartDownloads.Core;
using System.Collections.Concurrent;
using System.IO;

namespace SmartDownloads.Services;

public sealed class DownloadOrganizer : IDisposable
{
    private static readonly string[] PartialExtensions = [".crdownload", ".part", ".partial", ".tmp", ".download", ".opdownload"];
    private readonly string _downloads;
    private readonly IReadOnlyDictionary<DownloadCategory, string> _folders;
    private readonly LocalFileClassifier _classifier = new();
    private readonly AiClassificationClient? _ai;
    private readonly ConcurrentDictionary<string, Snapshot> _observations = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _scanLock = new(1, 1);
    private readonly FileSystemWatcher _watcher;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(12));
    private readonly CancellationTokenSource _stop = new();
    private readonly string _logFile;

    public DownloadOrganizer(string language, string? downloadsPath = null) : this(new AppSettings(language, true), downloadsPath, downloadsPath is null) { }

    public DownloadOrganizer(AppSettings settings, string? downloadsPath = null, bool enableAi = true)
    {
        _downloads = downloadsPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        _folders = FolderCatalog.ForLanguage(settings.Language);
        if (enableAi) _ai = new AiClassificationClient(settings);
        var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartDownloads");
        Directory.CreateDirectory(dataDir);
        _logFile = Path.Combine(dataDir, "organizer.log");
        foreach (var folder in _folders.Values) Directory.CreateDirectory(Path.Combine(_downloads, folder));
        _watcher = new FileSystemWatcher(_downloads) { IncludeSubdirectories = false, NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.LastWrite };
        _watcher.Created += OnChanged; _watcher.Changed += OnChanged; _watcher.Renamed += OnRenamed;
    }

    public void Start()
    {
        _watcher.EnableRaisingEvents = true;
        _ = RunLoopAsync(_stop.Token);
        Log("Organizer started.");
    }

    public Task ScanNowAsync(CancellationToken token = default) => ScanAsync(token);

    private void OnChanged(object sender, FileSystemEventArgs e) => _observations.TryRemove(e.FullPath, out _);
    private void OnRenamed(object sender, RenamedEventArgs e) { _observations.TryRemove(e.OldFullPath, out _); _observations.TryRemove(e.FullPath, out _); }

    private async Task RunLoopAsync(CancellationToken token)
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(token)) await ScanAsync(token);
        }
        catch (OperationCanceledException) { }
    }

    internal async Task ScanAsync(CancellationToken token = default)
    {
        if (!await _scanLock.WaitAsync(0, token)) return;
        try
        {
            foreach (var path in Directory.EnumerateFileSystemEntries(_downloads))
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    var item = Directory.Exists(path) ? (FileSystemInfo)new DirectoryInfo(path) : new FileInfo(path);
                    if (ShouldIgnore(item) || !IsStable(item)) continue;
                    var category = _ai is null ? null : await _ai.ClassifyAsync(item, token);
                    MoveSafely(item, category ?? _classifier.Classify(item));
                    _observations.TryRemove(path, out _);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException ex) { Log($"Access denied: {Path.GetFileName(path)} - {ex.Message}"); }
                catch (Exception ex) { Log($"Error: {Path.GetFileName(path)} - {ex.Message}"); }
            }
        }
        finally { _scanLock.Release(); }
    }

    private bool ShouldIgnore(FileSystemInfo item)
    {
        if (item.Name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase) || FolderCatalog.IsManagedFolder(item.Name)) return true;
        if (item is FileInfo && PartialExtensions.Contains(item.Extension, StringComparer.OrdinalIgnoreCase)) return true;
        if (DateTime.UtcNow - item.LastWriteTimeUtc < TimeSpan.FromSeconds(20)) return true;
        if (item is DirectoryInfo directory)
        {
            try { return directory.EnumerateFiles("*", SearchOption.AllDirectories).Any(x => PartialExtensions.Contains(x.Extension, StringComparer.OrdinalIgnoreCase)); }
            catch { return true; }
        }
        return false;
    }

    private bool IsStable(FileSystemInfo item)
    {
        item.Refresh();
        var current = new Snapshot(item is FileInfo file ? file.Length : -1, item.LastWriteTimeUtc);
        if (!_observations.TryGetValue(item.FullName, out var previous) || previous != current)
        {
            _observations[item.FullName] = current;
            return false;
        }
        if (item is FileInfo)
        {
            try { using var stream = File.Open(item.FullName, FileMode.Open, FileAccess.Read, FileShare.None); }
            catch { return false; }
        }
        return true;
    }

    private void MoveSafely(FileSystemInfo item, DownloadCategory category)
    {
        var destinationDirectory = Path.Combine(_downloads, _folders[category]);
        var destination = Path.Combine(destinationDirectory, item.Name);
        if (File.Exists(destination) || Directory.Exists(destination))
        {
            var baseName = item is FileInfo ? Path.GetFileNameWithoutExtension(item.Name) : item.Name;
            var extension = item is FileInfo ? item.Extension : string.Empty;
            var number = 2;
            do { destination = Path.Combine(destinationDirectory, $"{baseName} ({number++}){extension}"); } while (File.Exists(destination) || Directory.Exists(destination));
        }
        if (item is FileInfo) File.Move(item.FullName, destination); else Directory.Move(item.FullName, destination);
        Log($"{item.Name} -> {category}");
    }

    private void Log(string message)
    {
        try { File.AppendAllText(_logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}{Environment.NewLine}"); } catch { }
    }

    public void Dispose()
    {
        _stop.Cancel(); _watcher.Dispose(); _timer.Dispose(); _scanLock.Dispose(); _stop.Dispose(); _ai?.Dispose();
        Log("Organizer stopped.");
    }

    private readonly record struct Snapshot(long Length, DateTime LastWriteUtc);
}
