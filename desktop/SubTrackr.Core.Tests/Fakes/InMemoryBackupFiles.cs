using System.Text;
using SubTrackr.Core.Backup;

namespace SubTrackr.Core.Tests.Fakes;

/// <summary>Backup "files" kept in a dictionary by path.</summary>
public sealed class InMemoryBackupFiles : IBackupFiles
{
    public Dictionary<string, string> Files { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string? ReadText(string path, int maxBytes)
    {
        if (!Files.TryGetValue(path, out var text))
        {
            throw new FileNotFoundException("No such backup.", path);
        }

        return Encoding.UTF8.GetByteCount(text) > maxBytes ? null : text;
    }

    public void WriteText(string path, string text) => Files[path] = text;
}
