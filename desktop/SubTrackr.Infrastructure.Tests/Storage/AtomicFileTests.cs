using SubTrackr.Infrastructure.Storage;
using SubTrackr.Infrastructure.Tests.Fakes;

namespace SubTrackr.Infrastructure.Tests.Storage;

public class AtomicFileTests
{
    [Fact]
    public void WriteAllText_TargetLocked_KeepsOldContentAndCleansUp()
    {
        using var folder = new TemporaryDirectory();
        var path = folder.File("data.json");
        File.WriteAllText(path, "old");

        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Assert.ThrowsAny<IOException>(() => AtomicFile.WriteAllText(path, "new"));
        }

        Assert.Equal("old", File.ReadAllText(path));
        Assert.False(File.Exists(path + ".tmp"));
    }
}
