using System.Windows;
using SubTrackr.Desktop.Services;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Shell;

/// <summary>The nav rail and the three pages; everything else lives in the view models.</summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DarkTitleBar.Apply(this);
        DataContext = viewModel;
    }
}
