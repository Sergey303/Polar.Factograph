using Polar.Factograph.Api.Writing;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ResourceWriteRequestMapperTests
{
    [Fact]
    public void Map_PreservesLiteralResourceAndExplicitIdentity()
    {
        ResourceWriteBody body = new(
            "http://fogid.net/o/person",
            [
                new ResourcePropertyWriteBody(
                    "http://fogid.net/o/name",
                    "literal",
                    "Alice",
                    Language: "ru"),
                new ResourcePropertyWriteBody(
                    "http://fogid.net/o/friend",
                    "RESOURCE",
                    "target",
                    DataType: null)
            ],
            CassetteId: "current");

        ProjectResourceWriteCommand command = new ResourceWriteRequestMapper()
            .Map(body, "person-1");

        Assert.Equal("current", command.CassetteId);
        Assert.Equal("person-1", command.Resource.ResourceId);
        Assert.Equal("http://fogid.net/o/person", command.Resource.TypeId);
        Assert.Collection(
            command.Resource.Properties,
            property =>
            {
                Assert.Equal(FogPropertyKind.Literal, property.Kind);
                Assert.Equal("Alice", property.Value);
                Assert.Equal("ru", property.Language);
            },
            property =>
            {
                Assert.Equal(FogPropertyKind.Resource, property.Kind);
                Assert.Equal("target", property.Value);
            });
    }

    [Fact]
    public void Map_RejectsUnknownPropertyKind()
    {
        ResourceWriteBody body = new(
            "person",
            [new ResourcePropertyWriteBody("name", "unknown", "Alice")]);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new ResourceWriteRequestMapper().Map(body));

        Assert.Contains("Unknown resource property kind", exception.Message);
    }

    [Fact]
    public void Map_RejectsMissingProperties()
    {
        ResourceWriteBody body = new("person", Properties: null);

        Assert.Throws<ArgumentException>(() =>
            new ResourceWriteRequestMapper().Map(body));
    }
}
