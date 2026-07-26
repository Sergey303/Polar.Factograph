using Polar.Factograph.Fog;
using Polar.Factograph.Storage;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectResourceTargetValidationSuccessTests
{
    [Fact]
    public async Task WriteAsync_AllowsInheritedAndExactTargetRanges()
    {
        await using WritableApiProjectFixture fixture =
            await WritableApiProjectFixture.CreateAsync();
        using WritableApiMutationHarness harness = new();
        await harness.RebuildAsync(fixture.Project);
        FogResourceWriteRequest request = new(
            "person",
            [
                new FogProperty("name", FogPropertyKind.Literal, "Alice"),
                new FogProperty("mentor", FogPropertyKind.Resource, "target"),
                new FogProperty("employer", FogPropertyKind.Resource, "company")
            ]);

        var outcome = await harness.Resources.WriteAsync(
            fixture.Context,
            request,
            requestedCassetteId: null);

        Assert.True(outcome.IndexReady);
        using PolarDbTypedProjectStore store = PolarDbTypedProjectStore.OpenCurrent(
            fixture.Project.Index.Path);
        Assert.NotNull(await store.GetResourceHeadAsync(outcome.ResourceId));
    }

    [Fact]
    public async Task WriteAsync_AllowsExplicitSelfReferenceWithoutCurrentIndex()
    {
        await using WritableApiProjectFixture fixture =
            await WritableApiProjectFixture.CreateAsync();
        using WritableApiMutationHarness harness = new();
        FogResourceWriteRequest request = new(
            "person",
            [new FogProperty("mentor", FogPropertyKind.Resource, "self")],
            ResourceId: "self");

        var outcome = await harness.Resources.WriteAsync(
            fixture.Context,
            request,
            requestedCassetteId: null);

        Assert.True(outcome.IndexReady);
        Assert.Equal("self", outcome.ResourceId);
    }
}
