namespace SubTrackr.Core.Auth;

/// <summary>The outcome of a sign-in step, with a failure the UI can show in plain words.</summary>
public sealed record SignInResult(bool Succeeded, string Error)
{
    public static SignInResult Success { get; } = new(true, "");

    public static SignInResult Failed(string error) => new(false, error);
}
