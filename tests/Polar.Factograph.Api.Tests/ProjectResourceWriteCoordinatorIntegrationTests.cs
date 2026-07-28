using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Fog;
using Polar.Factograph.Storage;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectResourceWriteCoordinatorIntegrationTests
{
    private const string FogNamespace = "http://fogid.net/o/";

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

    [Fact]
    public async Task WriteAsync_AllowsExistingLegacyPropertyWithOriginalKind()
    {
        await using WritableApiProjectFixture fixture =
            await WritableApiProjectFixture.CreateAsync();
        using WritableApiMutationHarness harness = new();
        await harness.RebuildAsync(fixture.Project);
        FogResourceWriteRequest request = new(
            "person",
            [
                new FogProperty("name", FogPropertyKind.Literal, "Updated"),
                new FogProperty(
                    FogNamespace + "height",
                    FogPropertyKind.Literal,
                    "2600")
            ],
            ResourceId: "existing");

        ProjectResourceWriteOutcome outcome = await harness.Resources.WriteAsync(
            fixture.Context,
            request,
            requestedCassetteId: null);

        Assert.True(outcome.IndexReady);
        using PolarDbTypedProjectStore store = PolarDbTypedProjectStore.OpenCurrent(
            fixture.Project.Index.Path);
        List<TripleRow> triples = [];
        await foreach (TripleRow triple in store.FindAsync(
                           new TriplePattern(Subject: "existing"),
                           fixture.Context.Access.ReadableCassetteIds))
        {
            triples.Add(triple);
        }
        Assert.Contains(triples, triple =>
            triple.Predicate == FogNamespace + "height" &&
            triple.ObjectKind == TripleObjectKind.Literal &&
            triple.ObjectValue == "2600");
    }

    [Fact]
    public async Task WriteAsync_RejectsNewUnknownPropertyOnExistingResource()
    {
        await using WritableApiProjectFixture fixture =
            await WritableApiProjectFixture.CreateAsync();
        using WritableApiMutationHarness harness = new();
        await harness.RebuildAsync(fixture.Project);
        FogResourceWriteRequest request = new(
            "person",
            [
                new FogProperty("name", FogPropertyKind.Literal, "Updated"),
                new FogProperty(
                    FogNamespace + "width",
                    FogPropertyKind.Literal,
                    "3456")
            ],
            ResourceId: "existing");

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            harness.Resources.WriteAsync(
                fixture.Context,
                request,
                requestedCassetteId: null));

        Assert.Contains("width", exception.Message, StringComparison.Ordinal);
    }
}
