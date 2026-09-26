using SubTrackr.Core.Diagnostics;
using SubTrackr.Core.Subscriptions;
using SubTrackr.Core.Sync;

namespace SubTrackr.Core.Auth;

/// <summary>
/// Who syncs, with which project (SPEC.md section 8.2): the project settings, email-code sign-in,
/// the stored session and its refresh, and sign-out. A changed project URL or key signs out, a
/// refused refresh signs out, and sign-out is local first: the stored session goes even when the
/// logout call fails. Not thread-safe; the app uses it from the UI thread.
/// </summary>
public sealed class SyncAccount
{
    private readonly SubscriptionLedger ledger;
    private readonly ISupabaseAuth auth;
    private readonly ISessionStore sessions;
    private readonly TimeProvider time;
    private readonly IAppLog log;

    public SyncAccount(SubscriptionLedger ledger, ISupabaseAuth auth, ISessionStore sessions, TimeProvider time, IAppLog log)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(auth);
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(log);
        this.ledger = ledger;
        this.auth = auth;
        this.sessions = sessions;
        this.time = time;
        this.log = log;
    }

    /// <summary>After sign-in, sign-out, a refresh, or a project change.</summary>
    public event EventHandler? Changed;

    /// <summary>The configured project, or null when sync is off.</summary>
    public SupabaseProject? Project => SupabaseProject.TryCreate(ledger.Settings.SyncUrl, ledger.Settings.SyncKey);

    public AuthSession? Session { get; private set; }

    public bool IsSignedIn => Session is not null;

    /// <summary>Loads the stored session; one that belongs to another project is deleted.</summary>
    public void Restore()
    {
        var stored = sessions.Load();
        if (stored is not null && Project is { } project && stored.ProjectUrl == project.Url.AbsoluteUri)
        {
            Session = stored;
        }
        else if (stored is not null)
        {
            sessions.Delete();
            Session = null;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Saves the project URL and publishable key. Empty values turn sync off. Any change signs out
    /// first. Returns null when saved, otherwise what is wrong with the input.
    /// </summary>
    public string? SetProject(string url, string publishableKey)
    {
        var clearing = string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(publishableKey);
        var next = clearing ? null : SupabaseProject.TryCreate(url, publishableKey);
        if (!clearing && next is null)
        {
            return "Enter the project's https URL and its publishable key.";
        }

        // Compare the parsed projects, so "https://Project.example/" and "https://project.example" are one.
        var previousProject = Project;
        var settings = ledger.Settings;
        var unchanged = next is null
            ? settings.SyncUrl.Length == 0 && settings.SyncKey.Length == 0
            : next == previousProject;
        if (unchanged)
        {
            return null;
        }

        var previousSession = Session;
        DropSession();
        ledger.UpdateSettings(s =>
        {
            s.SyncUrl = next?.Url.AbsoluteUri.TrimEnd('/') ?? "";
            s.SyncKey = next?.PublishableKey ?? "";
        });
        Changed?.Invoke(this, EventArgs.Empty);
        _ = LogOutRemotelyAsync(previousProject, previousSession);
        return null;
    }

    public async Task<SignInResult> SendCodeAsync(string emailText, CancellationToken cancellationToken)
    {
        if (Project is not { } project)
        {
            return SignInResult.Failed("Save the project URL and key first.");
        }

        if (EmailAddress.Parse(emailText) is not { } email)
        {
            return SignInResult.Failed("Enter a valid email address.");
        }

        try
        {
            await auth.SendCodeAsync(project, email, cancellationToken);
            return SignInResult.Success;
        }
        catch (SyncException exception)
        {
            log.Error("Sending the sign-in code failed", exception);
            return SignInResult.Failed(exception.Failure switch
            {
                SyncFailure.Rejected or SyncFailure.Unauthorized or SyncFailure.Forbidden =>
                    "The project refused the request. Check the URL and key, and that email sign-in is on.",
                _ => Explain(exception.Failure),
            });
        }
    }

    public async Task<SignInResult> VerifyAsync(string emailText, string codeText, CancellationToken cancellationToken)
    {
        if (Project is not { } project)
        {
            return SignInResult.Failed("Save the project URL and key first.");
        }

        if (EmailAddress.Parse(emailText) is not { } email)
        {
            return SignInResult.Failed("Enter a valid email address.");
        }

        if (SignInCode.Parse(codeText) is not { } code)
        {
            return SignInResult.Failed($"Enter the {SignInCode.MinLength} to {SignInCode.MaxLength} digit code from the email.");
        }

        try
        {
            var tokens = await auth.VerifyCodeAsync(project, email, code, cancellationToken);
            Keep(AuthSession.From(tokens, project.Url.AbsoluteUri, time.GetUtcNow(), email.Value));
            log.Info("Signed in to sync.");
            return SignInResult.Success;
        }
        catch (SyncException exception)
        {
            log.Error("Verifying the sign-in code failed", exception);
            return SignInResult.Failed(exception.Failure switch
            {
                SyncFailure.Rejected or SyncFailure.Unauthorized or SyncFailure.Forbidden => "That code is wrong or has expired.",
                _ => Explain(exception.Failure),
            });
        }
    }

    /// <summary>Signs out here at once, then tells the server (best effort).</summary>
    public async Task SignOutAsync()
    {
        var project = Project;
        var session = Session;
        DropSession();
        Changed?.Invoke(this, EventArgs.Empty);
        await LogOutRemotelyAsync(project, session);
    }

    /// <summary>
    /// A session whose access token is good for at least another minute, refreshing it when it is
    /// not or when <paramref name="force"/> is set (after a 401). A refused refresh signs out and
    /// throws <see cref="NotSignedInException"/>; network trouble throws <see cref="SyncException"/>.
    /// </summary>
    public async Task<AuthSession> GetSessionAsync(bool force, CancellationToken cancellationToken)
    {
        if (Project is not { } project || Session is not { } session)
        {
            throw new NotSignedInException();
        }

        if (!force && !session.ExpiresSoon(time.GetUtcNow()))
        {
            return session;
        }

        AuthTokens tokens;
        try
        {
            tokens = await auth.RefreshAsync(project, session.RefreshToken, cancellationToken);
        }
        catch (SyncException exception) when (!exception.IsRetryable)
        {
            log.Error("The session could not be refreshed; signing out", exception);
            DropSession();
            Changed?.Invoke(this, EventArgs.Empty);
            throw new NotSignedInException("The sign-in expired. Sign in again.", exception);
        }

        Keep(AuthSession.From(tokens, project.Url.AbsoluteUri, time.GetUtcNow(), session.Email));
        return Session!;
    }

    private static string Explain(SyncFailure failure) => failure switch
    {
        SyncFailure.Network => "No connection. Check the network and try again.",
        SyncFailure.Timeout => "The project didn't answer in time. Try again.",
        SyncFailure.RateLimited => "Too many attempts. Wait a minute and try again.",
        SyncFailure.Server => "The project had a problem. Try again later.",
        _ => "The project's answer was unexpected. Try again.",
    };

    private void Keep(AuthSession session)
    {
        sessions.Save(session);
        Session = session;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void DropSession()
    {
        Session = null;
        sessions.Delete();
    }

    private async Task LogOutRemotelyAsync(SupabaseProject? project, AuthSession? session)
    {
        if (project is null || session is null)
        {
            return;
        }

        try
        {
            await auth.SignOutAsync(project, session.AccessToken, CancellationToken.None);
        }
        catch (Exception exception) when (exception is SyncException or OperationCanceledException)
        {
            // Sign-out is local first; the server session simply expires.
            log.Info("The server logout call failed: " + exception.Message);
        }
    }
}
