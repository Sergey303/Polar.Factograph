using Polar.Factograph.Api.Documents;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class DocumentVariantSelectorTests
{
    [Theory]
    [InlineData("original", DocumentVariant.Original, "original.jpg")]
    [InlineData("ICON", DocumentVariant.Icon, "icon.jpg")]
    [InlineData("SMALL", DocumentVariant.Small, "medium.jpg")]
    [InlineData("medium", DocumentVariant.Medium, "medium.jpg")]
    [InlineData("normal", DocumentVariant.Normal, "normal.jpg")]
    public void ParseAndSelect_ReturnsThePreferredPath(
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
    public void Select_SmallPrefersNormalBeforeLegacySmallWhenMediumIsMissing()
    {
        CassetteDocumentLocation location = CreateLocation() with
        {
            MediumPreviewPath = null
        };

        Assert.Equal(
            "normal.jpg",
            DocumentVariantSelector.Select(location, DocumentVariant.Small));
    }

    [Fact]
    public void Select_SmallFallsBackToLegacySmallWhenLargerPreviewsAreMissing()
    {
        CassetteDocumentLocation location = CreateLocation() with
        {
            MediumPreviewPath = null,
            NormalPreviewPath = null
        };

        Assert.Equal(
            "small.jpg",
            DocumentVariantSelector.Select(location, DocumentVariant.Small));
    }

    [Fact]
    public void Select_SmallFallsBackToIconWhenOnlyIconPreviewExists()
    {
        CassetteDocumentLocation location = CreateLocation() with
        {
            SmallPreviewPath = null,
            MediumPreviewPath = null,
            NormalPreviewPath = null
        };

        Assert.Equal(
            "icon.jpg",
            DocumentVariantSelector.Select(location, DocumentVariant.Small));
    }

    [Fact]
    public void Select_SmallFallsBackToOriginalWhenNoPreviewExists()
    {
        CassetteDocumentLocation location = CreateLocation() with
        {
            SmallPreviewPath = null,
            MediumPreviewPath = null,
            NormalPreviewPath = null,
            IconPreviewPath = null
        };

        Assert.Equal(
            "original.jpg",
            DocumentVariantSelector.Select(location, DocumentVariant.Small));
    }

    [Fact]
    public void Select_OriginalDoesNotFallBackToPreview()
    {
        CassetteDocumentLocation location = CreateLocation() with
        {
            OriginalPath = null
        };

        Assert.Null(DocumentVariantSelector.Select(location, DocumentVariant.Original));
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
