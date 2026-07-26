using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Api.Writing;
using Polar.Factograph.Application;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectResourceWriteCoordinatorRefreshTests
{
    [Fact]
    public async Task WriteAsync_ReturnsDedicatedStatusAfterWriterCompletes()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        FogSourceDescriptor source = ResourceWriteTestData.Source();
        RecordingFogResourceWriter writer = new(new FogResourceWriteResult(
            "p1",
            source.FogPath,
            2,
            new DateTime(2026, 7, 26, 8, 0, 0, DateTimeKind.Utc)));
        StubProjectIndexRefresher refresher = new(
            exception: new IOException("index unavailable"));
        ProjectResourceWriteCoordinator coordinator = new(
            new StubFogSourceScanner([source]),
            new ProjectWriteCassetteResolver(),
            writer,
            refresher,
            new ProjectMutationGate());

        try
        {
            ProjectWriteCommittedException status =
                await Assert.ThrowsAsync<ProjectWriteCommittedException>(() =>
                    coordinator.WriteAsync(
                        ResourceWriteTestData.Project(root),
                        ResourceWriteTestData.Access(),
                        ResourceWriteTestData.Command()));

            Assert.Equal("p1", status.ResourceId);
            Assert.IsType<IOException>(status.InnerException);
            Assert.Equal(1, writer.CallCount);
            Assert.Equal(1, refresher.CallCount);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
