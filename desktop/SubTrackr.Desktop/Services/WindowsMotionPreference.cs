using System.Windows;

namespace SubTrackr.Desktop.Services;

/// <summary>
/// Windows' "Animation effects" setting (Settings, Accessibility, Visual effects), which WPF reads
/// as <see cref="SystemParameters.ClientAreaAnimation"/>.
/// </summary>
public sealed class WindowsMotionPreference : IMotionPreference
{
    public bool AnimationsEnabled => SystemParameters.ClientAreaAnimation;
}
