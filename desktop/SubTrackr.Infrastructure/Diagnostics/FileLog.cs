using System.Globalization;
using SubTrackr.Core.Diagnostics;

namespace SubTrackr.Infrastructure.Diagnostics;

/// <summary>
/// Appends lines to <c>subtrackr.log</c> in the given folder. Past about 1 MB the file moves to
/// <c>subtrackr.old.log</c>, so at most two files ever exist. Exceptions are written as their type
/// and message, without stack data that could carry request contents. Logging never throws.
/// </summary>
public sealed class FileLog : IAppLog
{
    private const long MaxBytes = 1024 * 1024;

    private readonly object gate = new();
    private readonly string folder;
    private readonly TimeProvider time;

    public FileLog(string folder, TimeProvider time)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentNullException.ThrowIfNull(time);
        this.folder = folder;
        this.time = time;
    }

    public string FilePath => Path.Combine(folder, "subtrackr.log");

    public void Info(string message) => Write("INFO", message, null);

    public void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        try
        {
            var stamp = time.GetLocalNow().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            var line = $"{stamp} [{level}] {message}";
            for (var inner = exception; inner is not null; inner = inner.InnerException)
            {
                line += $" | {inner.GetType().Name}: {inner.Message}";
            }

            lock (gate)
            {
                Directory.CreateDirectory(folder);
                var info = new FileInfo(FilePath);
                if (info.Exists && info.Length > MaxBytes)
                {
                    File.Move(FilePath, Path.Combine(folder, "subtrackr.old.log"), overwrite: true);
                }

                File.AppendAllText(FilePath, line + "\n");
            }
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
            // Logging must never take the app down.
        }
    }
}
