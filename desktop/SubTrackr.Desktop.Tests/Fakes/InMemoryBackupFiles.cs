using System.Text;
using SubTrackr.Core.Backup;

namespace SubTrackr.Desktop.Tests.Fakes;

/// <summary>Backup "files" kept in a dictionary by path.</summary>
public sealed class InMemoryBackupFiles : IBackupFiles
{
    public Dictionary<string, string> Files { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string? ReadText(string path, int maxBytes) =>
        Files.TryGetValue(path, out var text)
            ? Encoding.UTF8.GetByteCount(text) > maxBytes ? null : text
            : throw new FileNotFoundException("No such backup.", path);

    public void WriteText(string path, string text) => Files[path] = text;
}
