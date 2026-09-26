using System.Windows;
using SubTrackr.Desktop.Theming;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Shell;

/// <summary>The nav rail and the three pages; everything else lives in the view models.</summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        TitleBarTheme.Attach(this);
        DataContext = viewModel;
    }
}
