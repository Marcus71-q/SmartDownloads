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
}
