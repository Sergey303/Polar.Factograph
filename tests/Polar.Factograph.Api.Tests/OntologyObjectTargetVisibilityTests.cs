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
        ResourceHead hidden = CreateHead("hidden", "secret");
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

    [Fact]
    public async Task ValidateAsync_AllowsAnyMatchingTargetType()
    {
        OntologyCatalog catalog = await OntologyWriteTestCatalog.CreateAsync();
        ResourceHead target = CreateHead("target", "current");
        FogResourceWriteRequest request = new(
            "child",
            [new FogProperty("employer", FogPropertyKind.Resource, "target")]);

        await new OntologyObjectTargetValidator().ValidateAsync(
            catalog,
            new ObjectTargetStoreStub(target, ["child", "organization"]),
            request,
            new HashSet<string>(["current"], StringComparer.Ordinal),
            CancellationToken.None);
    }

    private static ResourceHead CreateHead(string id, string cassetteId) => new(
        id,
        Guid.NewGuid(),
        cassetteId,
        $"{cassetteId}.fog",
        DateTimeOffset.UtcNow,
        IsDeleted: false);
}
