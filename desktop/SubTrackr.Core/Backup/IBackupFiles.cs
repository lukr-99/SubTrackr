namespace SubTrackr.Core.Backup;

/// <summary>Reads and writes the backup files the user picks.</summary>
public interface IBackupFiles
{
    /// <summary>The file's text, or null when it is larger than <paramref name="maxBytes"/>.</summary>
    string? ReadText(string path, int maxBytes);

    /// <summary>Writes the whole file at once; a failure leaves any existing file as it was.</summary>
    void WriteText(string path, string text);
}
