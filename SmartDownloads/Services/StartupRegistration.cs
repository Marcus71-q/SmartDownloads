using Microsoft.Win32;
using System.IO;
namespace SmartDownloads.Services;
public static class StartupRegistration
{
    public static void Enable()
    {
        try
        {
            var path = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(path) || !Path.GetFileName(path).Equals("SmartDownloads.exe", StringComparison.OrdinalIgnoreCase)) return;
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            key.SetValue("SmartDownloads", $"\"{path}\" --background", RegistryValueKind.String);
        }
        catch { }
    }

    public static void Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            key?.DeleteValue("SmartDownloads", false);
        }
        catch { }
    }

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue("SmartDownloads") is string;
        }
        catch { return false; }
    }
}
