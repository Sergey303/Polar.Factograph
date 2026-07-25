using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Storage.Tests;

public sealed class CurrentRecordTripleProjectorTests
{
    [Fact]
    public void Project_ProducesDeterministicHeadAndTriplesWithProvenance()
    {
        FogCurrentRecord record = new(
            "person-1",
            "http://fogid.net/o/person",
            new DateTime(2026, 7, 25, 8, 0, 0, DateTimeKind.Unspecified),
            new FogProperty[]
            {
                new(
                    "http://fogid.net/o/name",
                    FogPropertyKind.Literal,
                    "Иван",
                    Language: "ru"),
                new(
                    "http://fogid.net/o/friend",
                    FogPropertyKind.Resource,
                    "person-2")
            },
            "cassette-a",
            "CassetteA",
            "/data/CassetteA/meta/CassetteA_current.fog",
            42,
            IsSynthetic: false);

        CurrentRecordTripleProjector projector = new();
        ProjectedResource first = projector.Project(record);
        ProjectedResource second = projector.Project(record);

        Assert.Equal(first.Head, second.Head);
        Assert.Equal(first.Triples.ToArray(), second.Triples.ToArray());
        Assert.Equal("person-1", first.Head.ResourceId);
        Assert.Equal("cassette-a", first.Head.SourceCassetteId);
        Assert.Equal(3, first.Triples.Count);
        Assert.Equal(3, first.Triples.Select(triple => triple.TripleId).Distinct().Count());

        TripleRow type = first.Triples.Single(triple =>
            triple.Predicate == "http://www.w3.org/1999/02/22-rdf-syntax-ns#type");
        Assert.Equal(TripleObjectKind.Iri, type.ObjectKind);
        Assert.Equal("http://fogid.net/o/person", type.ObjectValue);

        TripleRow name = first.Triples.Single(triple => triple.Predicate == "http://fogid.net/o/name");
        Assert.Equal(TripleObjectKind.Literal, name.ObjectKind);
        Assert.Equal("ru", name.Language);

        TripleRow friend = first.Triples.Single(triple => triple.Predicate == "http://fogid.net/o/friend");
        Assert.Equal(TripleObjectKind.Iri, friend.ObjectKind);
        Assert.Equal("person-2", friend.ObjectValue);
        Assert.All(first.Triples, triple => Assert.Equal(first.Head.CurrentSourceRecordId, triple.SourceRecordId));
    }

    [Fact]
    public void Project_UsesStableSystemProvenanceForSyntheticRoot()
    {
        FogCurrentRecord root = new(
            LegacyFogVocabulary.CassetteRootCollectionId,
            LegacyFogVocabulary.Namespace + "collection",
            DateTime.MinValue,
            new[]
            {
                new FogProperty(
                    LegacyFogVocabulary.Namespace + "name",
                    FogPropertyKind.Literal,
                    "кассеты")
            },
            SourceCassetteId: null,
            SourceCassetteName: null,
            SourceFogPath: null,
            SourceOrdinal: null,
            IsSynthetic: true);

        ProjectedResource projected = new CurrentRecordTripleProjector().Project(root);

        Assert.Equal("$system", projected.Head.SourceCassetteId);
        Assert.Equal("$synthetic", projected.Head.SourceFogPath);
        Assert.All(projected.Triples, triple => Assert.Equal("$system", triple.SourceCassetteId));
    }
}
