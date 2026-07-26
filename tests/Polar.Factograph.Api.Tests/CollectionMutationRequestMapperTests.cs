using Polar.Factograph.Api.Collections;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class CollectionMutationRequestMapperTests
{
    [Fact]
    public void Normalize_CleansLegacyIdentifierSeparators()
    {
        CollectionItemRemoveRequest normalized =
            CollectionMutationRequestMapper.Normalize(
                new CollectionItemRemoveRequest(
                    "member|1",
                    "collection|1",
                    "item|1",
                    "current"));

        Assert.Equal("member1", normalized.MembershipResourceId);
        Assert.Equal("collection1", normalized.CollectionId);
        Assert.Equal("item1", normalized.ResourceId);
        Assert.Equal("current", normalized.CassetteId);
    }

    [Fact]
    public void Normalize_RejectsBlankIdentifier()
    {
        Assert.Throws<ArgumentException>(() =>
            CollectionMutationRequestMapper.Normalize(
                new CollectionItemAddRequest("collection-1", " ")));
    }
}
