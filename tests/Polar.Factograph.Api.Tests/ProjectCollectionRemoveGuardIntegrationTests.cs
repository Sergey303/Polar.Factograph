using Polar.Factograph.Api.Collections;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectCollectionRemoveGuardIntegrationTests
{
    [Fact]
    public async Task RemoveAsync_MismatchedItemDoesNotChangeFogOrDirtyIndex()
    {
        await using WritableApiProjectFixture fixture =
            await WritableApiProjectFixture.CreateAsync();
        using WritableApiMutationHarness harness = new();
        await harness.RebuildAsync(fixture.Project);
        CollectionItemMutationResponse added = await harness.CollectionsAdd.AddAsync(
            fixture.Context,
            new CollectionItemAddRequest("collection-1", "target"));
        string fogPath = Path.Combine(
            fixture.Root,
            "Cassette",
            "meta",
            "Cassette_current.fog");
        string before = await File.ReadAllTextAsync(fogPath);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            harness.CollectionsRemove.RemoveAsync(
                fixture.Context,
                new CollectionItemRemoveRequest(
                    added.MembershipResourceId,
                    "collection-1",
                    "existing")));

        Assert.Equal(before, await File.ReadAllTextAsync(fogPath));
        Assert.False(harness.DirtyMarker.Exists(fixture.Project.Index.Path));
    }
}
