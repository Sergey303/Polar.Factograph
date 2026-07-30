using Polar.Factograph.Api.Documents;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class DocumentVariantSelectorTests
{
    [Theory]
    [InlineData("original", DocumentVariant.Original, "original.jpg")]
    [InlineData("ICON", DocumentVariant.Icon, "icon.jpg")]
    [InlineData("SMALL", DocumentVariant.Small, "small.jpg")]
    [InlineData("medium", DocumentVariant.Medium, "medium.jpg")]
    [InlineData("normal", DocumentVariant.Normal, "normal.jpg")]
    public void ParseAndSelect_ReturnsTheRequestedPath(
        string value,
        DocumentVariant expectedVariant,
        string expectedPath)
    {
        CassetteDocumentLocation location = CreateLocation();
        DocumentVariant variant = DocumentVariantSelector.Parse(value);

        Assert.Equal(expectedVariant, variant);
        Assert.Equal(expectedPath, DocumentVariantSelector.Select(location, variant));
    }

    [Fact]
    public void Select_UsesSmallPreviewWhenIconIsMissing()
    {
        CassetteDocumentLocation location = CreateLocation() with
        {
            IconPreviewPath = null
        };

        Assert.Equal(
            "small.jpg",
            DocumentVariantSelector.Select(location, DocumentVariant.Icon));
    }

    [Fact]
    public void Parse_RejectsUnknownVariant()
    {
        Assert.Throws<ArgumentException>(() => DocumentVariantSelector.Parse("thumbnail"));
    }

    private static CassetteDocumentLocation CreateLocation() => new(
        "cassette-id",
        "Cassette",
        "iiss://Cassette@host/0001/0002",
        "0001",
        "0002",
        "original.jpg",
        "small.jpg",
        "medium.jpg",
        "normal.jpg")
    {
        IconPreviewPath = "icon.jpg"
    };
}
