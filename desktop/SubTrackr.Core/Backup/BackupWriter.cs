using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Google.Protobuf;
using SubTrackr.Core.Contracts;

namespace SubTrackr.Core.Backup;

/// <summary>
/// Writes the backup file (SPEC.md section 9.1): the envelope around the database in the proto3
/// JSON mapping with default values, so 64-bit integers are strings. Every subscription goes in,
/// tombstones included; <c>syncUrl</c> and <c>syncKey</c> never do.
/// </summary>
public static class BackupWriter
{
    private static readonly JsonFormatter Formatter = new(JsonFormatter.Settings.Default.WithFormatDefaultValues(true));

    private static readonly JsonSerializerOptions Output = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Write(Database database, DateTimeOffset exportedAt, string appVersion)
    {
        ArgumentNullException.ThrowIfNull(database);
        var copy = database.Clone();
        copy.Settings ??= new Settings();

        var body = JsonNode.Parse(Formatter.Format(copy))!.AsObject();
        if (body["settings"] is JsonObject settings)
        {
            settings.Remove("syncUrl");
            settings.Remove("syncKey");
        }

        var envelope = new JsonObject
        {
            ["format"] = BackupFormat.Name,
            ["formatVersion"] = BackupFormat.Version,
            ["exportedAt"] = exportedAt.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            ["appVersion"] = appVersion,
            ["platform"] = BackupFormat.Platform,
            ["database"] = body,
        };
        return envelope.ToJsonString(Output) + "\n";
    }
}
