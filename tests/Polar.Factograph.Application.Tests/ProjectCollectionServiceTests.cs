using Polar.Factograph.Application;
using Polar.Factograph.Storage;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class ProjectCollectionServiceTests
{
    [Fact]
    public async Task GetAsync_JoinsMembershipRelationsAndOrdersItemsByName()
    {
        CollectionRdfStoreStub rdf = new(
            [
                CollectionTestData.Head("collection-1", "cass-a"),
                CollectionTestData.Head("item-1", "cass-a"),
                CollectionTestData.Head("item-2", "cass-a")
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
                    "cass-a"),
                CollectionTestData.Link(
                    "membership-2",
                    CollectionTestData.InCollection,
                    "collection-1",
                    "cass-a"),
                CollectionTestData.Link(
                    "membership-2",
                    CollectionTestData.CollectionItem,
                    "item-2",
                    "cass-a"),
                CollectionTestData.Link(
                    "item-1",
                    CollectionTestData.RdfType,
                    "person",
                    "cass-a")
            ]);
        CollectionSearchStoreStub search = new(new Dictionary<string, IReadOnlyList<NameSearchHit>>
        {
            ["item-1"] = [CollectionTestData.NameHit("item-1", "Бета", "cass-a")],
            ["item-2"] = [CollectionTestData.NameHit("item-2", "Альфа", "cass-a")]
        });
        ProjectCollectionService service = new(rdf, search);

        ProjectCollectionContents? contents = await service.GetAsync(
            "collection-1",
            new HashSet<string>(StringComparer.Ordinal) { "cass-a" });

        Assert.NotNull(contents);
        Assert.Equal(["item-2", "item-1"], contents.Items.Select(item => item.ResourceId));
        Assert.Equal("person", contents.Items[1].Type);
        Assert.Equal("membership-1", contents.Items[1].MembershipResourceId);
    }
}
