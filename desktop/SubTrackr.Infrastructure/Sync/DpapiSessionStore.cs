using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SubTrackr.Core.Auth;

namespace SubTrackr.Infrastructure.Sync;

/// <summary>
/// Keeps the sign-in session in one file encrypted with DPAPI for the current Windows user, so
/// another account on the PC, or a copied file, cannot use it. A file that cannot be decrypted or
/// read is deleted and counts as signed out.
/// </summary>
public sealed class DpapiSessionStore(string path) : ISessionStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("SubTrackr.Session.v1");

    public AuthSession? Load()
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = ProtectedData.Unprotect(File.ReadAllBytes(path), Entropy, DataProtectionScope.CurrentUser);
            var session = JsonSerializer.Deserialize<AuthSession>(json);
            if (session is { AccessToken.Length: > 0, RefreshToken.Length: > 0, UserId.Length: > 0 })
            {
                return session;
            }
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException or IOException)
        {
            // Another user's file, or damaged: treat as signed out.
        }

        Delete();
        return null;
    }

    public void Save(AuthSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var protectedBytes = ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(session), Entropy, DataProtectionScope.CurrentUser);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporary = path + ".tmp";
        File.WriteAllBytes(temporary, protectedBytes);
        File.Move(temporary, path, overwrite: true);
    }

    public void Delete()
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
