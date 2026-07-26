using Polar.Factograph.Fog;
using Polar.Factograph.Storage;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectSubstituteCoordinatorIntegrationTests
{
    [Fact]
    public async Task SubstituteAsync_RedirectsOldIdInCurrentGeneration()
    {
        await using WritableApiProjectFixture fixture =
            await WritableApiProjectFixture.CreateAsync();
        using WritableApiMutationHarness harness = new();

        var outcome = await harness.Directives.SubstituteAsync(
            fixture.Context,
            new FogDirectiveWriteRequest(
                FogRecordKind.Substitute,
                "existing",
                "target"),
            requestedCassetteId: null);

        Assert.True(outcome.IndexReady);
        Assert.Equal("target", outcome.SubstituteTargetId);
        Assert.False(harness.DirtyMarker.Exists(fixture.Project.Index.Path));
        using PolarDbTypedProjectStore store = PolarDbTypedProjectStore.OpenCurrent(
            fixture.Project.Index.Path);
        Assert.Null(await store.GetResourceHeadAsync("existing"));
        Assert.NotNull(await store.GetResourceHeadAsync("target"));
    }
}
