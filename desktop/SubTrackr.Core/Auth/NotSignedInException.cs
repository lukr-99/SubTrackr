namespace SubTrackr.Core.Auth;

/// <summary>Sync needs a signed-in user and there is none (never signed in, or the refresh was refused).</summary>
public sealed class NotSignedInException : Exception
{
    public NotSignedInException()
        : base("Not signed in.")
    {
    }

    public NotSignedInException(string message)
        : base(message)
    {
    }

    public NotSignedInException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
