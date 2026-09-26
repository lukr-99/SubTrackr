using System.Text;
using SubTrackr.Core.Auth;
using SubTrackr.Infrastructure.Sync;
using SubTrackr.Infrastructure.Tests.Fakes;

namespace SubTrackr.Infrastructure.Tests.Sync;

public class DpapiSessionStoreTests
{
    private static readonly AuthSession Session = new(
        "https://project.example/",
        "access-token-value",
        "refresh-token-value",
        new DateTimeOffset(2026, 9, 26, 11, 0, 0, TimeSpan.Zero),
        "9b2e6f3a-5c1d-4e8f-a7b6-0c1d2e3f4a5b",
        "user@example.com");

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        using var folder = new TemporaryDirectory();
        var store = new DpapiSessionStore(folder.File("session.bin"));

        store.Save(Session);

        Assert.Equal(Session, store.Load());
    }

    [Fact]
    public void Save_FileIsEncrypted()
    {
        using var folder = new TemporaryDirectory();
        var store = new DpapiSessionStore(folder.File("session.bin"));

        store.Save(Session);

        var bytes = File.ReadAllBytes(folder.File("session.bin"));
        foreach (var secret in new[] { "access-token-value", "refresh-token-value", "user@example.com" })
        {
            Assert.DoesNotContain(secret, Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);
            Assert.DoesNotContain(secret, Encoding.Unicode.GetString(bytes), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Load_DamagedFile_IsDeletedAndSignedOut()
    {
        using var folder = new TemporaryDirectory();
        File.WriteAllBytes(folder.File("session.bin"), [1, 2, 3, 4]);
        var store = new DpapiSessionStore(folder.File("session.bin"));

        Assert.Null(store.Load());
        Assert.False(File.Exists(folder.File("session.bin")));
    }

    [Fact]
    public void Delete_RemovesTheFile()
    {
        using var folder = new TemporaryDirectory();
        var store = new DpapiSessionStore(folder.File("session.bin"));
        store.Save(Session);

        store.Delete();

        Assert.Null(store.Load());
        Assert.False(File.Exists(folder.File("session.bin")));
    }
}
