using System.Windows;
using SubTrackr.Core.Contracts;
using SubTrackr.Desktop.ViewModels;
using SubTrackr.Desktop.Views;

namespace SubTrackr.Desktop.Services;

/// <summary>Message boxes and the edit window, owned by the app's main window.</summary>
public sealed class WpfDialogService : IDialogService
{
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

    private static Window Owner() => Application.Current.MainWindow;
}
