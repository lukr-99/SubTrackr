namespace SubTrackr.Infrastructure.Storage;

/// <summary>
/// Where SubTrackr keeps its files: <c>%APPDATA%\SubTrackr</c>. Installer updates and uninstalling
/// leave the folder alone; deleting it resets the app.
/// </summary>
public sealed class AppDataPaths
{
    public AppDataPaths(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        Root = root;
    }

    public string Root { get; }

    public string DataFile => Path.Combine(Root, "data.json");

    public string LogsFolder => Path.Combine(Root, "logs");

    public static AppDataPaths ForUser() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SubTrackr"));
}
