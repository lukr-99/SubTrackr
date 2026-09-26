using Microsoft.Extensions.Time.Testing;
using SubTrackr.Infrastructure.Diagnostics;
using SubTrackr.Infrastructure.Tests.Fakes;

namespace SubTrackr.Infrastructure.Tests.Diagnostics;

public class FileLogTests
{
    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 9, 26, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Error_WritesMessageAndExceptionChain()
    {
        using var folder = new TemporaryDirectory();
        var log = new FileLog(folder.File("logs"), time);

        log.Error("Sync failed", new InvalidOperationException("outer", new TimeoutException("inner")));

        var text = File.ReadAllText(log.FilePath);
        Assert.Equal("2026-09-26 10:00:00 [ERROR] Sync failed | InvalidOperationException: outer | TimeoutException: inner\n", text);
    }

    [Fact]
    public void Info_PastSizeLimit_RollsToOldFile()
    {
        using var folder = new TemporaryDirectory();
        var log = new FileLog(folder.Path, time);
        File.WriteAllText(log.FilePath, new string('x', 1024 * 1024 + 1));

        log.Info("fresh");

        Assert.True(File.Exists(folder.File("subtrackr.old.log")));
        Assert.Equal("2026-09-26 10:00:00 [INFO] fresh\n", File.ReadAllText(log.FilePath));
    }
}
