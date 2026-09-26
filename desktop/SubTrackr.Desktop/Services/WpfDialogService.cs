using System.Windows;
using Microsoft.Win32;
using SubTrackr.Core.Contracts;
using SubTrackr.Desktop.ViewModels;
using SubTrackr.Desktop.Views;

namespace SubTrackr.Desktop.Services;

/// <summary>Message boxes, file pickers, and the edit window, owned by the app's main window.</summary>
public sealed class WpfDialogService : IDialogService
{
    private const string JsonFilter = "SubTrackr backup (*.json)|*.json|All files (*.*)|*.*";

    public bool Confirm(string message, string title) =>
        MessageBox.Show(Owner(), message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public void Inform(string message, string title) =>
        MessageBox.Show(Owner(), message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public void Warn(string message, string title) =>
        MessageBox.Show(Owner(), message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

    public Subscription? EditSubscription(EditSubscriptionViewModel editor)
    {
        var window = new EditSubscriptionWindow(editor) { Owner = Owner() };
        return window.ShowDialog() == true ? window.Result : null;
    }

    public string? PickSaveFile(string suggestedName)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Back up SubTrackr",
            FileName = suggestedName,
            DefaultExt = ".json",
            Filter = JsonFilter,
            AddExtension = true,
            OverwritePrompt = true,
        };
        return dialog.ShowDialog(Owner()) == true ? dialog.FileName : null;
    }

    public string? PickOpenFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Restore a SubTrackr backup",
            Filter = JsonFilter,
            CheckFileExists = true,
            Multiselect = false,
        };
        return dialog.ShowDialog(Owner()) == true ? dialog.FileName : null;
    }

    private static Window Owner() => Application.Current.MainWindow;
}
