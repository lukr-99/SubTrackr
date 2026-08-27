using System.Windows;
using SubTrackr.Core.Contracts;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop;

public partial class EditSubscriptionWindow : Window
{
    private readonly EditSubscriptionViewModel _vm;

    /// <summary>The validated subscription, available after the dialog returns true.</summary>
    public Subscription? Result { get; private set; }

    public EditSubscriptionWindow(EditSubscriptionViewModel vm)
    {
        InitializeComponent();
        Services.DarkTitleBar.Apply(this);
        _vm = vm;
        DataContext = vm;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.TryBuild() is { } sub)
        {
            Result = sub;
            DialogResult = true;
        }
        // else: _vm.Error is now set and shown; keep the dialog open.
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Icon_Pick(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button b && b.Content is string emoji)
            _vm.Icon = emoji;
        IconToggle.IsChecked = false;
    }
}
