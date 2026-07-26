using Polar.Factograph.Fog;
using Polar.Factograph.Storage;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectDeleteCoordinatorIntegrationTests
{
    [Fact]
    public async Task DeleteAsync_RemovesResourceFromCurrentGeneration()
    {
        await using WritableApiProjectFixture fixture =
            await WritableApiProjectFixture.CreateAsync();
        using WritableApiMutationHarness harness = new();

        var outcome = await harness.Directives.DeleteAsync(
            fixture.Context,
            new FogDirectiveWriteRequest(FogRecordKind.Delete, "existing"),
            requestedCassetteId: null);

        Assert.True(outcome.IndexReady);
        Assert.False(harness.DirtyMarker.Exists(fixture.Project.Index.Path));
        using PolarDbTypedProjectStore store = PolarDbTypedProjectStore.OpenCurrent(
            fixture.Project.Index.Path);
        Assert.Null(await store.GetResourceHeadAsync("existing"));
        Assert.NotNull(await store.GetResourceHeadAsync("target"));
    }
}
