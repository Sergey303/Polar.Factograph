using Polar.Factograph.Api.Writes;
using Polar.Factograph.Application;
using Polar.Factograph.Fog;
using Polar.Factograph.Storage;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class OntologyObjectTargetVisibilityTests
{
    [Fact]
    public async Task ValidateAsync_HidesTargetOutsideReadableCassettes()
    {
        OntologyCatalog catalog = await OntologyWriteTestCatalog.CreateAsync();
        ResourceHead hidden = new(
            "hidden",
            Guid.NewGuid(),
            "secret",
            "secret.fog",
            DateTimeOffset.UtcNow,
            IsDeleted: false);
        FogResourceWriteRequest request = new(
            "child",
            [new FogProperty("mentor", FogPropertyKind.Resource, "hidden")]);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            new OntologyObjectTargetValidator().ValidateAsync(
                catalog,
                new ObjectTargetStoreStub(hidden),
                request,
                new HashSet<string>(["current"], StringComparer.Ordinal),
                CancellationToken.None));

        Assert.Contains("does not exist or is not readable", exception.Message, StringComparison.Ordinal);
    }
}
