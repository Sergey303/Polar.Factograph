using Polar.Factograph.Api.Collections;
using Polar.Factograph.Application;
using Polar.Factograph.Storage;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectCollectionRemoveCoordinatorIntegrationTests
{
    [Fact]
    public async Task RemoveAsync_DeletesOnlyTheMatchingMembership()
    {
        await using WritableApiProjectFixture fixture =
            await WritableApiProjectFixture.CreateAsync();
        using WritableApiMutationHarness harness = new();
        await harness.RebuildAsync(fixture.Project);
        CollectionItemMutationResponse added = await harness.CollectionsAdd.AddAsync(
            fixture.Context,
            new CollectionItemAddRequest("collection-1", "target"));

        CollectionItemMutationResponse removed = await harness.CollectionsRemove.RemoveAsync(
            fixture.Context,
            new CollectionItemRemoveRequest(
                added.MembershipResourceId,
                "collection-1",
                "target"));

        Assert.True(removed.IndexReady);
        using PolarDbTypedProjectStore store = PolarDbTypedProjectStore.OpenCurrent(
            fixture.Project.Index.Path);
        ProjectCollectionContents? contents = await new ProjectCollectionService(store, store)
            .GetAsync(
                "collection-1",
                new HashSet<string>(["current"], StringComparer.Ordinal));
        Assert.Empty(contents!.Items);
        Assert.NotNull(await store.GetResourceHeadAsync("target"));
    }
}
