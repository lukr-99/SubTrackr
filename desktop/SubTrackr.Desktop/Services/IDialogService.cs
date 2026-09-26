using SubTrackr.Core.Contracts;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Services;

/// <summary>
/// Modal questions and dialogs the view models ask for. The WPF implementation shows real
/// windows; tests answer with a fake so nothing appears on screen.
/// </summary>
public interface IDialogService
{
    /// <summary>Asks a yes-or-no question; true when the user agrees.</summary>
    bool Confirm(string message, string title);

    void Inform(string message, string title);

    void Warn(string message, string title);

    /// <summary>Shows the add/edit form; the finished subscription, or null when cancelled.</summary>
    Subscription? EditSubscription(EditSubscriptionViewModel editor);
}
