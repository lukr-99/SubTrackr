using System.Windows;
using SubTrackr.Desktop.Controls;
using SubTrackr.Desktop.Services;
using SubTrackr.Desktop.Theming;
using SubTrackr.Desktop.ViewModels;

namespace SubTrackr.Desktop.Shell;

/// <summary>
/// The nav rail and the three pages; everything else lives in the view models. The first time the
/// window loads, the logo in the rail plays its launch animation unless Windows asks for reduced
/// motion (SPEC.md section 10).
/// </summary>
public partial class MainWindow : Window
{
    private readonly IMotionPreference motion;

    public MainWindow(MainViewModel viewModel, IMotionPreference motion)
    {
        ArgumentNullException.ThrowIfNull(motion);
        this.motion = motion;
        InitializeComponent();
        TitleBarTheme.Attach(this);
        DataContext = viewModel;
        Loaded += OnFirstLoaded;
    }

    /// <summary>The mark at the top of the nav rail.</summary>
    public LogoMark Logo => LaunchMark;

    private void OnFirstLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnFirstLoaded;
        if (motion.AnimationsEnabled)
        {
            LaunchMark.PlayLaunchAnimation();
        }
    }
}
