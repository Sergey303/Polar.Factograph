using Polar.Factograph.Api.Infrastructure;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectMutationGateTests
{
    [Fact]
    public async Task AcquireAsync_WaitsUntilTheCurrentLeaseIsReleased()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "polar-factograph-gate-tests",
            Guid.NewGuid().ToString("N"));
        ProjectMutationGate gate = new();

        try
        {
            await using (await gate.AcquireAsync(root))
            {
                using CancellationTokenSource cancellation = new(
                    TimeSpan.FromMilliseconds(150));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    gate.AcquireAsync(root, cancellation.Token));
            }

            await using ProjectMutationLease next = await gate.AcquireAsync(root);
            Assert.NotNull(next);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
