using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectResourceWriteValidationIntegrationTests
{
    [Fact]
    public async Task WriteAsync_InvalidOntologyPropertyDoesNotChangeFogOrDirtyIndex()
    {
        await using WritableApiProjectFixture fixture =
            await WritableApiProjectFixture.CreateAsync();
        using WritableApiMutationHarness harness = new();
        string fogPath = Path.Combine(
            fixture.Root,
            "Cassette",
            "meta",
            "Cassette_current.fog");
        string before = await File.ReadAllTextAsync(fogPath);
        FogResourceWriteRequest request = new(
            "person",
            [new FogProperty("unknown", FogPropertyKind.Literal, "value")]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            harness.Resources.WriteAsync(
                fixture.Context,
                request,
                requestedCassetteId: null));

        Assert.Equal(before, await File.ReadAllTextAsync(fogPath));
        Assert.False(harness.DirtyMarker.Exists(fixture.Project.Index.Path));
    }
}
