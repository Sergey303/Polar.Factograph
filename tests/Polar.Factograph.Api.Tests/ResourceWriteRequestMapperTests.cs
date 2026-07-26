using Polar.Factograph.Api.Writes;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ResourceWriteRequestMapperTests
{
    [Fact]
    public void Map_PreservesLiteralAndResourceProperties()
    {
        ResourceWriteRequest request = new(
            "person",
            [
                new ResourceWritePropertyRequest(
                    "name",
                    "Alice",
                    Language: "ru"),
                new ResourceWritePropertyRequest(
                    "friend",
                    "person-2",
                    Kind: "resource"),
                new ResourceWritePropertyRequest(
                    "score",
                    "42",
                    DataType: "http://www.w3.org/2001/XMLSchema#integer")
            ],
            ResourceId: "person-1",
            CassetteId: "current");

        FogResourceWriteRequest mapped = ResourceWriteRequestMapper.Map(request);

        Assert.Equal("person", mapped.TypeId);
        Assert.Equal("person-1", mapped.ResourceId);
        Assert.Equal(3, mapped.Properties.Count);
        Assert.Equal(FogPropertyKind.Literal, mapped.Properties[0].Kind);
        Assert.Equal("ru", mapped.Properties[0].Language);
        Assert.Equal(FogPropertyKind.Resource, mapped.Properties[1].Kind);
        Assert.Equal(
            "http://www.w3.org/2001/XMLSchema#integer",
            mapped.Properties[2].DataType);
    }

    [Fact]
    public void Map_RejectsUnknownPropertyKind()
    {
        ResourceWriteRequest request = new(
            "person",
            [new ResourceWritePropertyRequest("name", "Alice", Kind: "unknown")]);

        Assert.Throws<ArgumentException>(() =>
            ResourceWriteRequestMapper.Map(request));
    }
}
