using Polar.Factograph.Api.Collections;
using Polar.Factograph.Application;
using Polar.Factograph.Storage;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectCollectionAddCoordinatorIntegrationTests
{
    [Fact]
    public async Task AddAsync_CreatesVisibleCollectionMembership()
    {
        await using WritableApiProjectFixture fixture =
            await WritableApiProjectFixture.CreateAsync();
        using WritableApiMutationHarness harness = new();
        await harness.RebuildAsync(fixture.Project);

        CollectionItemMutationResponse response = await harness.CollectionsAdd.AddAsync(
            fixture.Context,
            new CollectionItemAddRequest("collection-1", "target"));

        Assert.True(response.IndexReady);
        using PolarDbTypedProjectStore store = PolarDbTypedProjectStore.OpenCurrent(
            fixture.Project.Index.Path);
        ProjectCollectionContents? contents = await new ProjectCollectionService(store, store)
            .GetAsync(
                "collection-1",
                new HashSet<string>(["current"], StringComparer.Ordinal));
        ProjectCollectionItem item = Assert.Single(contents!.Items);
        Assert.Equal(response.MembershipResourceId, item.MembershipResourceId);
        Assert.Equal("target", item.ResourceId);
    }
}
