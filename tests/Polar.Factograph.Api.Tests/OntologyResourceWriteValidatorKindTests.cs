using Polar.Factograph.Api.Writes;
using Polar.Factograph.Application;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class OntologyResourceWriteValidatorKindTests
{
    [Fact]
    public async Task Validate_RejectsUnknownClass()
    {
        OntologyCatalog catalog = await OntologyWriteTestCatalog.CreateAsync();
        FogResourceWriteRequest request = new("unknown", []);

        Assert.Throws<ArgumentException>(() =>
            new OntologyResourceWriteValidator().Validate(catalog, request));
    }

    [Fact]
    public async Task Validate_RejectsResourceValueForDatatypeProperty()
    {
        OntologyCatalog catalog = await OntologyWriteTestCatalog.CreateAsync();
        FogResourceWriteRequest request = new(
            "child",
            [new FogProperty("name", FogPropertyKind.Resource, "person-2")]);

        Assert.Throws<ArgumentException>(() =>
            new OntologyResourceWriteValidator().Validate(catalog, request));
    }

    [Fact]
    public async Task Validate_RejectsLiteralValueForObjectProperty()
    {
        OntologyCatalog catalog = await OntologyWriteTestCatalog.CreateAsync();
        FogResourceWriteRequest request = new(
            "child",
            [new FogProperty("mentor", FogPropertyKind.Literal, "person-2")]);

        Assert.Throws<ArgumentException>(() =>
            new OntologyResourceWriteValidator().Validate(catalog, request));
    }
}
