namespace SubTrackr.Core.Updates;

/// <summary>The outcome of a check; <see cref="Offer"/> is set when an update is available.</summary>
public sealed record UpdateCheckResult(UpdateStatus Status, UpdateOffer? Offer = null, string? Error = null);
