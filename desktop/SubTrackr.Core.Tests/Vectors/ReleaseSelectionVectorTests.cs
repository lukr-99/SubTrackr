using System.Text.Json;
using SubTrackr.Core.Updates;

namespace SubTrackr.Core.Tests.Vectors;

/// <summary>Runs contracts/vectors/release-selection.json (the file Android runs too).</summary>
public class ReleaseSelectionVectorTests
{
    private static readonly string VectorPath =
        Path.Combine(AppContext.BaseDirectory, "vectors", "release-selection.json");

    public static IEnumerable<object[]> Cases()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(VectorPath));
        foreach (var testCase in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            yield return [testCase.GetProperty("id").GetString()!, testCase.GetRawText()];
        }
    }

    public static IEnumerable<object[]> ChecksumFiles()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(VectorPath));
        foreach (var file in document.RootElement.GetProperty("checksumFiles").EnumerateArray())
        {
            var expected = file.GetProperty("expected");
            yield return
            [
                file.GetProperty("id").GetString()!,
                file.GetProperty("text").GetString()!,
                expected.ValueKind == JsonValueKind.Null ? null! : expected.GetString()!,
            ];
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Select_MatchesVector(string id, string json)
    {
        using var document = JsonDocument.Parse(json);
        var testCase = document.RootElement;
        var release = testCase.GetProperty("release");
        var published = new PublishedRelease(
            release.GetProperty("tag_name").GetString()!,
            release.GetProperty("assets").EnumerateArray()
                .Select(a => new ReleaseAsset(a.GetProperty("name").GetString()!, a.GetProperty("browser_download_url").GetString()!))
                .ToList());
        var platform = testCase.GetProperty("platform").GetString() == "desktop" ? UpdatePlatform.Desktop : UpdatePlatform.Android;

        var offer = ReleaseSelection.Select(published, testCase.GetProperty("current").GetString()!, platform);

        var expected = testCase.GetProperty("expected");
        Assert.True(expected.GetProperty("offer").GetBoolean() == offer is not null, id);
        if (offer is not null)
        {
            Assert.Equal(expected.GetProperty("version").GetString(), offer.Version.ToString());
            Assert.Equal(expected.GetProperty("asset").GetString(), offer.AssetName);
            Assert.Equal(expected.GetProperty("checksumAsset").GetString(), offer.ChecksumAssetName);
        }
    }

    [Theory]
    [MemberData(nameof(ChecksumFiles))]
    public void ChecksumFile_Parse_MatchesVector(string id, string text, string? expected)
    {
        Assert.True(expected == ChecksumFile.Parse(text), id);
    }
}
