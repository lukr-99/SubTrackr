namespace SubTrackr.Core.Updates;

/// <summary>A downloaded update could not be trusted, so it was not kept.</summary>
public sealed class UpdateVerificationException : Exception
{
    public UpdateVerificationException()
    {
    }

    public UpdateVerificationException(string message)
        : base(message)
    {
    }

    public UpdateVerificationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
