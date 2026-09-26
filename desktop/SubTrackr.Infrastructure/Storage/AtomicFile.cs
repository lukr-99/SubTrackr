using System.Text;

namespace SubTrackr.Infrastructure.Storage;

/// <summary>Replaces a file's text in one step: write a sibling temporary file, then swap it in.</summary>
public static class AtomicFile
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    public static void WriteAllText(string path, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(text);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, text, Utf8);
            if (File.Exists(path))
            {
                File.Replace(temporary, path, destinationBackupFileName: null);
            }
            else
            {
                File.Move(temporary, path);
            }
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
}
