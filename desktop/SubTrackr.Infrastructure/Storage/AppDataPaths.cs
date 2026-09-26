namespace SubTrackr.Infrastructure.Storage;

/// <summary>
/// Where SubTrackr keeps its files: <c>%APPDATA%\SubTrackr</c> for releases and
/// <c>%APPDATA%\SubTrackr Dev</c> for <c>-dev</c> builds, so trying a build never touches the
/// installed app's data (SPEC.md section 7). Installer updates and uninstalling leave the folder
/// alone; deleting it resets the app.
/// </summary>
public sealed class AppDataPaths
{
    public const string ReleaseFolderName = "SubTrackr";
    public const string DevFolderName = "SubTrackr Dev";

    public AppDataPaths(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        Root = root;
    }

    public string Root { get; }

    public string DataFile => Path.Combine(Root, "data.json");

    public string LogsFolder => Path.Combine(Root, "logs");

    public static AppDataPaths ForUser(bool isDevBuild) => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        isDevBuild ? DevFolderName : ReleaseFolderName));
}
