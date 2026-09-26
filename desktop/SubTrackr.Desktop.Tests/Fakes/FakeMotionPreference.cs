using SubTrackr.Desktop.Services;

namespace SubTrackr.Desktop.Tests.Fakes;

/// <summary>A motion setting the test picks; off by default so renders show the finished logo.</summary>
public sealed class FakeMotionPreference : IMotionPreference
{
    public bool AnimationsEnabled { get; set; }
}
