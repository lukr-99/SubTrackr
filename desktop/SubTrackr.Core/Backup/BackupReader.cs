using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Google.Protobuf;
using SubTrackr.Core.Contracts;

namespace SubTrackr.Core.Backup;

/// <summary>
/// Reads and validates a backup (SPEC.md section 9.2) before anything changes. The text is
/// untrusted: it is size-checked, parsed, and every subscription checked; 64-bit integers may be
/// strings or numbers, unknown fields are ignored, and missing fields take proto defaults.
/// </summary>
public static partial class BackupReader
{
    private static readonly JsonParser Parser = new(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

    public static BackupReadResult Read(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (Encoding.UTF8.GetByteCount(text) > BackupFormat.MaxBytes)
        {
            return BackupReadResult.Invalid(BackupError.InvalidJson, "larger than 10 MB");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            return BackupReadResult.Invalid(BackupError.InvalidJson, "not JSON");
        }

        using (document)
        {
            return Read(document.RootElement);
        }
    }

    private static BackupReadResult Read(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return BackupReadResult.Invalid(BackupError.InvalidJson, "not a JSON object");
        }

        if (!root.TryGetProperty("format", out var format) || format.ValueKind != JsonValueKind.String || format.GetString() != BackupFormat.Name)
        {
            return BackupReadResult.Invalid(BackupError.UnsupportedFormat, "not a SubTrackr backup");
        }

        if (!root.TryGetProperty("formatVersion", out var version) || version.ValueKind != JsonValueKind.Number
            || !version.TryGetInt64(out var number) || number != BackupFormat.Version)
        {
            return BackupReadResult.Invalid(BackupError.UnsupportedVersion, "unsupported format version");
        }

        if (!root.TryGetProperty("database", out var databaseElement) || databaseElement.ValueKind != JsonValueKind.Object)
        {
            return BackupReadResult.Invalid(BackupError.InvalidRecord, "no database");
        }

        Database database;
        try
        {
            database = Parser.Parse<Database>(databaseElement.GetRawText());
        }
        catch (Exception exception) when (exception is InvalidProtocolBufferException or InvalidJsonException or FormatException)
        {
            return BackupReadResult.Invalid(BackupError.InvalidRecord, "the database does not match the data shape");
        }

        return FindInvalidRecord(database) is { } problem
            ? BackupReadResult.Invalid(BackupError.InvalidRecord, problem)
            : BackupReadResult.Valid(database);
    }

    /// <summary>The first subscription rule a database breaks, or null when all hold.</summary>
    public static string? FindInvalidRecord(Database database)
    {
        ArgumentNullException.ThrowIfNull(database);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var subscription in database.Subscriptions)
        {
            if (!UuidPattern().IsMatch(subscription.Id))
            {
                return "a subscription ID is not a UUID";
            }

            if (!ids.Add(subscription.Id))
            {
                return "two subscriptions share an ID";
            }

            if (!CurrencyPattern().IsMatch(subscription.Cost?.Currency ?? ""))
            {
                return $"\"{subscription.Name}\" has no valid currency code";
            }

            if (subscription.Cost!.Exponent is < 0 or > 4)
            {
                return $"\"{subscription.Name}\" has an amount with an unsupported number of decimals";
            }

            if (string.IsNullOrEmpty(subscription.UpdatedAt))
            {
                return $"\"{subscription.Name}\" has no change time";
            }
        }

        return null;
    }

    [GeneratedRegex("^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", RegexOptions.CultureInvariant)]
    private static partial Regex UuidPattern();

    [GeneratedRegex("^[A-Z]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex CurrencyPattern();
}
