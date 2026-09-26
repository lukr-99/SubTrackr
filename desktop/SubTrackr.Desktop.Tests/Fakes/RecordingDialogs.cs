using SubTrackr.Core.Contracts;
using SubTrackr.Desktop.Services;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Tests.Fakes;

/// <summary>Answers dialogs the way the test says and remembers what was asked.</summary>
public sealed class RecordingDialogs : IDialogService
{
    public bool ConfirmAnswer { get; set; } = true;

    /// <summary>What the edit form returns; by default it saves whatever the editor builds.</summary>
    public Func<EditSubscriptionViewModel, Subscription?> EditAnswer { get; set; } = editor => editor.TryBuild();

    public List<string> Questions { get; } = [];

    public List<string> Messages { get; } = [];

    public bool Confirm(string message, string title)
    {
        Questions.Add(message);
        return ConfirmAnswer;
    }

    public void Inform(string message, string title) => Messages.Add(message);

    public void Warn(string message, string title) => Messages.Add(message);

    public Subscription? EditSubscription(EditSubscriptionViewModel editor) => EditAnswer(editor);
}
