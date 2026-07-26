using Polar.Factograph.Api.Infrastructure;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectOperationGateTests
{
    [Fact]
    public async Task AcquireAsync_SerializesOperationsForOneIndex()
    {
        ProjectOperationGate gate = new();
        await using IAsyncDisposable first = await gate.AcquireAsync("index-a");
        using CancellationTokenSource cancellation = new(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await gate.AcquireAsync("index-a", cancellation.Token));
    }

    [Fact]
    public async Task AcquireAsync_AllowsDifferentIndexes()
    {
        ProjectOperationGate gate = new();
        await using IAsyncDisposable first = await gate.AcquireAsync("index-a");
        await using IAsyncDisposable second = await gate.AcquireAsync("index-b");

        Assert.NotNull(first);
        Assert.NotNull(second);
    }
}
