using System.Text.Json;
using SubTrackr.Core.Contracts;
using SubTrackr.Core.Sync;

namespace SubTrackr.Core.Tests;

/// <summary>Runs the shared merge contract: contracts/vectors/merge.json (same file Android runs).</summary>
public class MergeVectorTests
{
    private static readonly string VectorPath =
        Path.Combine(AppContext.BaseDirectory, "vectors", "merge.json");

    public record Rec(string Id, string UpdatedAt, string DeletedAt);
    public record Case(string Id, Rec[] Local, Rec[] Remote, Rec[] Expected);
    public record VectorFile(Case[] Cases);

    public static IEnumerable<object[]> Cases()
    {
        var doc = JsonSerializer.Deserialize<VectorFile>(
            File.ReadAllText(VectorPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        foreach (var c in doc.Cases) yield return new object[] { c };
    }

    private static Subscription ToSub(Rec r) =>
        new() { Id = r.Id, UpdatedAt = r.UpdatedAt, DeletedAt = r.DeletedAt };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Merge_matches_vector(Case c)
    {
        var merged = MergeEngine.Merge(c.Local.Select(ToSub), c.Remote.Select(ToSub));
        var actual = merged.Select(s => new Rec(s.Id, s.UpdatedAt, s.DeletedAt)).ToArray();

        Assert.Equal(c.Expected.Length, actual.Length);
        for (var i = 0; i < actual.Length; i++)
        {
            Assert.Equal(c.Expected[i].Id, actual[i].Id);
            Assert.Equal(c.Expected[i].UpdatedAt, actual[i].UpdatedAt);
            Assert.Equal(c.Expected[i].DeletedAt, actual[i].DeletedAt);
        }
    }
}
