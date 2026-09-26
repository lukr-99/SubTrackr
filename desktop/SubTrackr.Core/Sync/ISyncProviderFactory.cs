namespace SubTrackr.Core.Sync;

/// <summary>Creates the sync transport for the project the user configured.</summary>
public interface ISyncProviderFactory
{
    ISyncProvider Create(string projectUrl, string publishableKey);
}
