using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectResourceTargetValidationFailureTests
{
    [Fact]
    public async Task WriteAsync_MissingTargetDoesNotChangeFogOrDirtyIndex()
    {
        await AssertRejectedWithoutMutationAsync(
            new FogProperty("mentor", FogPropertyKind.Resource, "missing"));
    }

    [Fact]
    public async Task WriteAsync_IncompatibleTargetTypeDoesNotChangeFogOrDirtyIndex()
    {
        await AssertRejectedWithoutMutationAsync(
            new FogProperty("employer", FogPropertyKind.Resource, "existing"));
    }

    private static async Task AssertRejectedWithoutMutationAsync(FogProperty property)
    {
        await using WritableApiProjectFixture fixture =
            await WritableApiProjectFixture.CreateAsync();
        using WritableApiMutationHarness harness = new();
        await harness.RebuildAsync(fixture.Project);
        string fogPath = Path.Combine(
            fixture.Root,
            "Cassette",
            "meta",
            "Cassette_current.fog");
        string before = await File.ReadAllTextAsync(fogPath);
        FogResourceWriteRequest request = new("person", [property]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            harness.Resources.WriteAsync(
                fixture.Context,
                request,
                requestedCassetteId: null));

        Assert.Equal(before, await File.ReadAllTextAsync(fogPath));
        Assert.False(harness.DirtyMarker.Exists(fixture.Project.Index.Path));
    }
}
