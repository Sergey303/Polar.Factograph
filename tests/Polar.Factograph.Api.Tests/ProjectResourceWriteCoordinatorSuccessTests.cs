using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Api.Writing;
using Polar.Factograph.Application;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectResourceWriteCoordinatorSuccessTests
{
    [Fact]
    public async Task WriteAsync_WritesAuthorizedFogThenRefreshesIndex()
    {
        string indexRoot = TemporaryIndexRoot();
        try
        {
            FogSourceDescriptor source = ResourceWriteTestData.Source();
            StubFogSourceScanner scanner = new([source]);
            DateTime modifiedAt = new(2026, 7, 26, 8, 0, 0, DateTimeKind.Utc);
            RecordingFogResourceWriter writer = new(new FogResourceWriteResult(
                "p1",
                source.FogPath,
                NextCounter: 2,
                modifiedAt));
            Guid generationId = Guid.NewGuid();
            StubProjectIndexRefresher refresher = new(
                ResourceWriteTestData.RebuildResult(generationId));
            ProjectResourceWriteCoordinator coordinator = new(
                scanner,
                new ProjectWriteCassetteResolver(),
                writer,
                refresher,
                new ProjectMutationGate());

            ProjectResourceWriteResult result = await coordinator.WriteAsync(
                ResourceWriteTestData.Project(indexRoot),
                ResourceWriteTestData.Access(),
                ResourceWriteTestData.Command());

            Assert.Equal("p1", result.ResourceId);
            Assert.Equal("current", result.CassetteId);
            Assert.Equal(modifiedAt, result.ModifiedAtUtc);
            Assert.Equal(generationId, result.GenerationId);
            Assert.Equal(2, result.SourceFiles);
            Assert.Equal(1, scanner.CallCount);
            Assert.Equal(1, writer.CallCount);
            Assert.Equal(1, refresher.CallCount);
            Assert.Same(source, writer.Source);
        }
        finally
        {
            Delete(indexRoot);
        }
    }

    private static string TemporaryIndexRoot() => Path.Combine(
        Path.GetTempPath(),
        "polar-factograph-api-tests",
        Guid.NewGuid().ToString("N"));

    private static void Delete(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
    }
}
