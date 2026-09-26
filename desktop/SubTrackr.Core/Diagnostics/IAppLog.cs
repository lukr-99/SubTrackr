namespace SubTrackr.Core.Diagnostics;

/// <summary>
/// Where the app notes what went wrong. Messages must never carry tokens, keys, or subscription
/// data; exceptions are logged for their type and message.
/// </summary>
public interface IAppLog
{
    void Info(string message);

    void Error(string message, Exception? exception = null);
}
