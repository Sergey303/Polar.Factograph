using Microsoft.Extensions.Logging.Abstractions;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
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
        FileSystemFogSourceScanner scanner = new();
        ProjectOperationGate gate = new();
        ProjectIndexDirtyMarker dirty = new();
        ProjectIndexCoordinator indexCoordinator = new(
            scanner,
            new FogProjectRecordSource(new FileSystemFogRecordReader()),
            new LegacyFogProjectMaterializer(),
            new ProjectIndexRebuilder(),
            gate,
            dirty);
        ProjectWriteIndexRefresher refresher = new(
            indexCoordinator,
            dirty,
            NullLogger<ProjectWriteIndexRefresher>.Instance);
        ProjectResourceWriteCoordinator coordinator = new(
            scanner,
            new FileSystemFogResourceWriter(),
            new ProjectWriteCassetteResolver(),
            gate,
            dirty,
            refresher);
        FogResourceWriteRequest request = new(
            "person",
            [new FogProperty("name", FogPropertyKind.Literal, "Alice")]);

        ProjectResourceWriteOutcome outcome = await coordinator.WriteAsync(
            fixture.Context,
            request,
            requestedCassetteId: null);

        Assert.True(outcome.IndexReady);
        Assert.Equal("p1", outcome.ResourceId);
        Assert.NotNull(outcome.GenerationId);
        Assert.False(dirty.Exists(fixture.Project.Index.Path));
        using PolarDbTypedProjectStore store = PolarDbTypedProjectStore.OpenCurrent(
            fixture.Project.Index.Path);
        ResourceHead? head = await store.GetResourceHeadAsync("p1");
        Assert.NotNull(head);
        Assert.Equal("current", head.SourceCassetteId);
    }
}
