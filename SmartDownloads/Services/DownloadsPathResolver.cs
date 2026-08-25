using System.Runtime.InteropServices;
using System.IO;

namespace SmartDownloads.Services;

public static class DownloadsPathResolver
{
    private static readonly Guid DownloadsFolderId = new("374DE290-123F-4565-9164-39C4925E467B");

    public static string Resolve()
    {
        IntPtr pointer = IntPtr.Zero;
        try
        {
            if (SHGetKnownFolderPath(DownloadsFolderId, 0, IntPtr.Zero, out pointer) == 0)
            {
                var path = Marshal.PtrToStringUni(pointer);
                if (!string.IsNullOrWhiteSpace(path)) return path;
            }
        }
        catch { }
        finally { if (pointer != IntPtr.Zero) Marshal.FreeCoTaskMem(pointer); }
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }

    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint flags, IntPtr token, out IntPtr path);
}
