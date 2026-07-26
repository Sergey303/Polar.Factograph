using Polar.Factograph.Api.Writes;
using Polar.Factograph.Application;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class OntologyResourceWriteValidatorValidTests
{
    [Fact]
    public async Task Validate_AllowsInheritedLiteralObjectAndEnumerationProperties()
    {
        OntologyCatalog catalog = await OntologyWriteTestCatalog.CreateAsync();
        FogResourceWriteRequest request = new(
            LegacyFogVocabulary.Namespace + "child",
            [
                new FogProperty(
                    LegacyFogVocabulary.Namespace + "name",
                    FogPropertyKind.Literal,
                    "Alice"),
                new FogProperty("mentor", FogPropertyKind.Resource, "person-2"),
                new FogProperty("status", FogPropertyKind.Literal, "active")
            ]);

        new OntologyResourceWriteValidator().Validate(catalog, request);
    }
}
