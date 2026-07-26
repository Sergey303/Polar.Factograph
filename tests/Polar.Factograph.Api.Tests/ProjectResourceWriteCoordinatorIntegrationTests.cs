using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Fog;
using Polar.Factograph.Storage;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectResourceWriteCoordinatorIntegrationTests
{
    [Fact]
    public async Task WriteAsync_WritesFogRebuildsIndexAndExposesResource()
    {
        await using WritableApiProjectFixture fixture =
            await WritableApiProjectFixture.CreateAsync();
        using WritableApiMutationHarness harness = new();
        FogResourceWriteRequest request = new(
            "person",
            [new FogProperty("name", FogPropertyKind.Literal, "Alice")]);

        ProjectResourceWriteOutcome outcome = await harness.Resources.WriteAsync(
            fixture.Context,
            request,
            requestedCassetteId: null);

        Assert.True(outcome.IndexReady);
        Assert.Equal("p1", outcome.ResourceId);
        Assert.NotNull(outcome.GenerationId);
        Assert.False(harness.DirtyMarker.Exists(fixture.Project.Index.Path));
        using PolarDbTypedProjectStore store = PolarDbTypedProjectStore.OpenCurrent(
            fixture.Project.Index.Path);
        ResourceHead? head = await store.GetResourceHeadAsync("p1");
        Assert.NotNull(head);
        Assert.Equal("current", head.SourceCassetteId);
    }
}
