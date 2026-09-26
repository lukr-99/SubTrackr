namespace SubTrackr.Core.Updates;

/// <summary>Whether the installer is running; when not, <see cref="Error"/> says why in plain words.</summary>
public sealed record UpdateInstallResult(bool Started, string? Error = null);
