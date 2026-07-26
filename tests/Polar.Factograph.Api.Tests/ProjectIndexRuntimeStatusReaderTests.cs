using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Storage;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectIndexRuntimeStatusReaderTests
{
    [Fact]
    public async Task Read_ReportsMissingBuildingAndReadyStates()
    {
        using IndexRuntimeStatusFixture fixture = IndexRuntimeStatusFixture.Create();
        ProjectIndexRuntimeStatusReader reader = new(new ProjectIndexDirtyMarker());

        ProjectIndexRuntimeStatus missing = reader.Read(fixture.Root);
        Assert.Equal("missing", missing.State);
        Assert.Equal("missing", missing.CurrentPointerState);

        Guid generationId = Guid.NewGuid();
        await using FileSystemIndexGeneration generation =
            FileSystemIndexGeneration.Begin(fixture.Root, generationId);
        ProjectIndexRuntimeStatus building = reader.Read(fixture.Root);
        Assert.Equal(1, building.BuildingGenerationCount);
        await generation.CommitAsync();

        ProjectIndexRuntimeStatus ready = reader.Read(fixture.Root);
        Assert.Equal("ready", ready.State);
        Assert.Equal("valid", ready.CurrentPointerState);
        Assert.Equal(generationId, ready.CurrentGenerationId);
        Assert.True(ready.CurrentGenerationAvailable);
        Assert.Equal(1, ready.CompletedGenerationCount);
        Assert.Equal(0, ready.BuildingGenerationCount);
    }

    [Fact]
    public async Task Read_ReportsDirtyMarkerTimestamp()
    {
        using IndexRuntimeStatusFixture fixture = IndexRuntimeStatusFixture.Create();
        await using FileSystemIndexGeneration generation =
            FileSystemIndexGeneration.Begin(fixture.Root);
        await generation.CommitAsync();
        ProjectIndexDirtyMarker marker = new();
        marker.Mark(fixture.Root);

        ProjectIndexRuntimeStatus status =
            new ProjectIndexRuntimeStatusReader(marker).Read(fixture.Root);

        Assert.Equal("dirty", status.State);
        Assert.True(status.Dirty);
        Assert.NotNull(status.DirtySinceUtc);
    }

    [Fact]
    public void Read_ReportsInvalidCurrentWithoutExposingPath()
    {
        using IndexRuntimeStatusFixture fixture = IndexRuntimeStatusFixture.Create();
        File.WriteAllText(Path.Combine(fixture.Root, "CURRENT"), "not-a-generation");
        Directory.CreateDirectory(
            Path.Combine(fixture.Root, $"generation-{Guid.NewGuid():N}.building"));

        ProjectIndexRuntimeStatus status =
            new ProjectIndexRuntimeStatusReader(new ProjectIndexDirtyMarker())
                .Read(fixture.Root);

        Assert.Equal("invalid", status.State);
        Assert.Equal("invalid", status.CurrentPointerState);
        Assert.Null(status.CurrentGenerationId);
        Assert.False(status.CurrentGenerationAvailable);
        Assert.Equal(1, status.BuildingGenerationCount);
    }
}
