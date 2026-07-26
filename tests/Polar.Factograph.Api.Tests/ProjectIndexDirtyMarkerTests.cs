using Polar.Factograph.Api.Infrastructure;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectIndexDirtyMarkerTests
{
    [Fact]
    public void MarkAndClear_ControlReadAvailability()
    {
        string root = CreateTemporaryDirectory();
        try
        {
            ProjectIndexDirtyMarker marker = new();
            ProjectStoreProvider provider = new(marker);

            marker.Mark(root);

            Assert.True(marker.Exists(root));
            Assert.Throws<ProjectRuntimeUnavailableException>(() =>
                provider.GetCurrent(root));

            marker.Clear(root);
            Assert.False(marker.Exists(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            "polar-factograph-api-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
