namespace SubTrackr.Desktop.ViewModels;

/// <summary>One trial or renewal alert.</summary>
public sealed record AlertLine(string Icon, string Title, string Detail, bool Urgent);
