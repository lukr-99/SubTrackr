namespace SubTrackr.Desktop.Services;

/// <summary>Whether the operating system lets apps play decorative motion (SPEC.md section 10).</summary>
public interface IMotionPreference
{
    /// <summary>False when the person asked for reduced motion.</summary>
    bool AnimationsEnabled { get; }
}
