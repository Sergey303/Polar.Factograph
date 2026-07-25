using Polar.Factograph.Application;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class ProjectCollectionVisibilityTests
{
    [Fact]
    public async Task GetAsync_DoesNotReadRelationsForForbiddenCollection()
    {
        CollectionRdfStoreStub rdf = new(
            [CollectionTestData.Head("collection-1", "cass-b")],
            []);
        ProjectCollectionService service = new(
            rdf,
            new CollectionSearchStoreStub(
                new Dictionary<string, IReadOnlyList<Polar.Factograph.Storage.NameSearchHit>>()));

        ProjectCollectionContents? contents = await service.GetAsync(
            "collection-1",
            new HashSet<string>(StringComparer.Ordinal) { "cass-a" });

        Assert.Null(contents);
        Assert.Equal(0, rdf.FindCalls);
    }

    [Fact]
    public async Task GetAsync_SkipsItemFromForbiddenCassette()
    {
        CollectionRdfStoreStub rdf = new(
            [
                CollectionTestData.Head("collection-1", "cass-a"),
                CollectionTestData.Head("item-1", "cass-b")
            ],
            [
                CollectionTestData.Link(
                    "membership-1",
                    CollectionTestData.InCollection,
                    "collection-1",
                    "cass-a"),
                CollectionTestData.Link(
                    "membership-1",
                    CollectionTestData.CollectionItem,
                    "item-1",
                    "cass-a")
            ]);
        ProjectCollectionService service = new(
            rdf,
            new CollectionSearchStoreStub(
                new Dictionary<string, IReadOnlyList<Polar.Factograph.Storage.NameSearchHit>>()));

        ProjectCollectionContents? contents = await service.GetAsync(
            "collection-1",
            new HashSet<string>(StringComparer.Ordinal) { "cass-a" });

        Assert.NotNull(contents);
        Assert.Empty(contents.Items);
    }
}
