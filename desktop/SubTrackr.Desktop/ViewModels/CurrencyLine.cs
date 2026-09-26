namespace SubTrackr.Desktop.ViewModels;

/// <summary>One currency in the "by currency" card.</summary>
public sealed record CurrencyLine(string Code, string MonthlyOwnText, string ConvertedText);
