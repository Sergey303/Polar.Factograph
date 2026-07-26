using Polar.Factograph.Api.Writes;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ResourceDirectiveRequestMapperTests
{
    [Fact]
    public void Map_CreatesDeleteDirective()
    {
        FogDirectiveWriteRequest mapped = ResourceDirectiveRequestMapper.Map(
            new ResourceDeleteRequest("resource-1", "current"));

        Assert.Equal(FogRecordKind.Delete, mapped.Kind);
        Assert.Equal("resource-1", mapped.ResourceId);
        Assert.Null(mapped.SubstituteTargetId);
    }

    [Fact]
    public void Map_CreatesSubstituteDirective()
    {
        FogDirectiveWriteRequest mapped = ResourceDirectiveRequestMapper.Map(
            new ResourceSubstituteRequest("old", "new", "current"));

        Assert.Equal(FogRecordKind.Substitute, mapped.Kind);
        Assert.Equal("old", mapped.ResourceId);
        Assert.Equal("new", mapped.SubstituteTargetId);
    }

    [Fact]
    public void Map_RejectsEmptyIdentifier()
    {
        Assert.Throws<ArgumentException>(() =>
            ResourceDirectiveRequestMapper.Map(new ResourceDeleteRequest(" ")));
    }
}
