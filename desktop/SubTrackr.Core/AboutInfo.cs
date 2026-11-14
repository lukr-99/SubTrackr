namespace SubTrackr.Core;

/// <summary>Author name and links shown on the About page. Single source: change them here only.</summary>
public static class AboutInfo
{
    public const string AuthorName = "Lukáš Krejčí";
    public const string InstagramUrl = "https://www.instagram.com/lukas9krejci/";
    public const string InstagramHandle = "@lukas9krejci";
    public const string GitHubUrl = "https://github.com/lukr-99";
    public const string GitHubHandle = "lukr-99";
    public const string RepoUrl = "https://github.com/lukr-99/SubTrackr";
    public const string IssuesUrl = RepoUrl + "/issues";
    public const string LicenseName = "PolyForm Noncommercial 1.0.0";
    public const string LicenseUrl = "https://polyformproject.org/licenses/noncommercial/1.0.0/";

    public sealed record Credit(string Name, string Note);

    /// <summary>Libraries this app ships. Taken from the project files.</summary>
    public static IReadOnlyList<Credit> Libraries { get; } =
    [
        new("CommunityToolkit.Mvvm", "MIT"),
        new("Google.Protobuf", "BSD 3-Clause"),
        new("DotNetLib.Core (self-update)", "by the same author"),
    ];

    /// <summary>Outside services the app talks to.</summary>
    public static IReadOnlyList<Credit> Services { get; } =
    [
        new("Frankfurter", "exchange rates"),
        new("Google favicon service", "service logos"),
        new("Supabase", "optional sync"),
    ];
}
