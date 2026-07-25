using Xunit;

namespace Polar.Factograph.Storage.Tests;

public sealed class LegacySearchIndexProjectorTests
{
    [Fact]
    public void Project_CreatesDeterministicNamePrefixesAndWordRows()
    {
        ProjectedResource resource = CreateResource(
            Literal(
                "10000000-0000-0000-0000-000000000001",
                LegacySearchIndexProjector.NamePredicate,
                "Anna-Maria Smith",
                "en"),
            Literal(
                "10000000-0000-0000-0000-000000000002",
                LegacySearchIndexProjector.DescriptionPredicate,
                "One, two two",
                null),
            Literal(
                "10000000-0000-0000-0000-000000000003",
                "http://fogid.net/o/not-searchable",
                "Ignored",
                null));
        LegacySearchIndexProjector projector = new();

        SearchIndexProjection first = projector.Project(resource);
        SearchIndexProjection second = projector.Project(resource);

        Assert.Equal(first, second);
        Assert.Contains(first.NameRows, row => row.SearchKey == "ANNA MARIA SMITH");
        Assert.Contains(first.NameRows, row => row.SearchKey == "MARIA");
        Assert.Contains(first.NameRows, row => row.SearchKey == "SMI");
        Assert.All(first.NameRows, row => Assert.Equal("en", row.Language));
        Assert.Equal(
            new[] { "ANNA", "MARIA", "ONE", "SMITH", "TWO" },
            first.WordRows.Select(row => row.Word).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(first.WordRows, row => row.Word == "IGNORED");
        Assert.Equal(first.NameRows.Count, first.NameRows.Select(row => row.SearchRowId).Distinct().Count());
        Assert.Equal(first.WordRows.Count, first.WordRows.Select(row => row.SearchRowId).Distinct().Count());
    }

    [Fact]
    public void QueryNormalization_UsesTheSameCanonicalTokensAsProjection()
    {
        Assert.Equal("ANNA MARIA", LegacySearchIndexProjector.NormalizeNameQuery("  Anna-Maria  "));
        Assert.Equal(
            new[] { "ONE", "TWO" },
            LegacySearchIndexProjector.NormalizeSearchWords("one, TWO two"));
        Assert.Empty(LegacySearchIndexProjector.CreateNameSearchKeys(" \t\r\n "));
    }

    private static ProjectedResource CreateResource(params TripleRow[] triples) => new(
        new ResourceHead(
            "resource-1",
            Guid.Parse("20000000-0000-0000-0000-000000000001"),
            "cassette-a",
            "/data/a.fog",
            DateTimeOffset.UnixEpoch,
            IsDeleted: false),
        triples);

    private static TripleRow Literal(
        string id,
        string predicate,
        string value,
        string? language) => new(
        Guid.Parse(id),
        "resource-1",
        predicate,
        TripleObjectKind.Literal,
        value,
        language,
        DataType: null,
        Guid.Parse("20000000-0000-0000-0000-000000000001"),
        "cassette-a",
        "/data/a.fog",
        DateTimeOffset.UnixEpoch);
}