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

    /// <summary>Libraries the Windows app ships, as listed in THIRD-PARTY-NOTICES.md.</summary>
    public static IReadOnlyList<AboutCredit> Libraries { get; } =
    [
        new("CommunityToolkit.Mvvm", "MIT"),
        new("Google.Protobuf", "BSD 3-Clause"),
        new("System.Security.Cryptography.ProtectedData", "MIT"),
        new(".NET runtime and WPF", "MIT"),
    ];

    /// <summary>Outside services the app talks to.</summary>
    public static IReadOnlyList<AboutCredit> Services { get; } =
    [
        new("Frankfurter", "exchange rates"),
        new("Google favicon service", "service logos"),
        new("Supabase", "optional sync"),
    ];
}
