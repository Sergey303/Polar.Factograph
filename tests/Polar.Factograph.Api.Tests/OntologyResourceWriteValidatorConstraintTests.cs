using Polar.Factograph.Api.Writes;
using Polar.Factograph.Application;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class OntologyResourceWriteValidatorConstraintTests
{
    [Fact]
    public async Task Validate_RejectsPropertyOutsideClassDomain()
    {
        OntologyCatalog catalog = await OntologyWriteTestCatalog.CreateAsync();
        FogResourceWriteRequest request = new(
            "other",
            [new FogProperty("name", FogPropertyKind.Literal, "Alice")]);

        Assert.Throws<ArgumentException>(() =>
            new OntologyResourceWriteValidator().Validate(catalog, request));
    }

    [Fact]
    public async Task Validate_RejectsUnknownEnumerationValue()
    {
        OntologyCatalog catalog = await OntologyWriteTestCatalog.CreateAsync();
        FogResourceWriteRequest request = new(
            "child",
            [new FogProperty("status", FogPropertyKind.Literal, "unknown")]);

        Assert.Throws<ArgumentException>(() =>
            new OntologyResourceWriteValidator().Validate(catalog, request));
    }

    [Fact]
    public async Task Validate_RejectsLanguageMetadataOnResourceValue()
    {
        OntologyCatalog catalog = await OntologyWriteTestCatalog.CreateAsync();
        FogResourceWriteRequest request = new(
            "child",
            [new FogProperty(
                "mentor",
                FogPropertyKind.Resource,
                "person-2",
                Language: "ru")]);

        Assert.Throws<ArgumentException>(() =>
            new OntologyResourceWriteValidator().Validate(catalog, request));
    }
}
