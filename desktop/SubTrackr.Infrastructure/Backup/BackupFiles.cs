using System.Text;
using SubTrackr.Core.Backup;
using SubTrackr.Infrastructure.Storage;

namespace SubTrackr.Infrastructure.Backup;

/// <summary>
/// Backup files on disk. Reading stops one byte past the limit, so an oversized file is refused
/// without loading it; writing goes through <see cref="AtomicFile"/>.
/// </summary>
public sealed class BackupFiles : IBackupFiles
{
    public string? ReadText(string path, int maxBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var buffer = new byte[maxBytes + 1];
        var total = 0;
        int read;
        while (total < buffer.Length && (read = stream.Read(buffer, total, buffer.Length - total)) > 0)
        {
            total += read;
        }

        if (total > maxBytes)
        {
            return null;
        }

        using var reader = new StreamReader(new MemoryStream(buffer, 0, total), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    public void WriteText(string path, string text) => AtomicFile.WriteAllText(path, text);
}
