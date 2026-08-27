using System.IO;

namespace SubTrackr.Desktop.Services;

/// <summary>Tiny rolling file logger under %AppData%\SubTrackr\logs (mirrors the house pattern).</summary>
public static class Log
{
    private static readonly object Gate = new();
    private static readonly string LogPath = BuildPath();

    private static string BuildPath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SubTrackr", "logs");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "subtrackr.log");
    }

    public static void Info(string message) => Write("INFO", message, null);
    public static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

    private static void Write(string level, string message, Exception? ex)
    {
        try
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}" +
                       (ex is null ? "" : $"\n{ex}") + "\n";
            lock (Gate) File.AppendAllText(LogPath, line);
        }
        catch { /* logging must never throw */ }
    }
}
