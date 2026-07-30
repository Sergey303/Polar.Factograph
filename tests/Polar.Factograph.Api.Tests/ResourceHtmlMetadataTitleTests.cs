using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ResourceHtmlMetadataTitleTests
{
    [Fact]
    public void TitleOf_HidesNameWhenResourceHasDocumentUri()
    {
        PresentedSemanticResourcePage page = Page(
            new PresentedResourceLiteralField(
                "http://fogid.net/o/name",
                "Название",
                "Скрытое название документа",
                "Скрытое название документа",
                "ru",
                null),
            new PresentedResourceLiteralField(
                "http://fogid.net/o/uri",
                "URI",
                "iiss://cassette/0001/0042",
                "iiss://cassette/0001/0042",
                null,
                null));

        string title = ResourceHtmlMetadataProvider.TitleOf(page);

        Assert.Equal("Фотодокумент", title);
        Assert.DoesNotContain("Скрытое", title, StringComparison.Ordinal);
    }

    [Fact]
    public void TitleOf_KeepsNameForOrdinaryEntity()
    {
        PresentedSemanticResourcePage page = Page(
            new PresentedResourceLiteralField(
                "http://fogid.net/o/name",
                "Название",
                "Обычная сущность",
                "Обычная сущность",
                "ru",
                null));

        Assert.Equal("Обычная сущность", ResourceHtmlMetadataProvider.TitleOf(page));
    }

    private static PresentedSemanticResourcePage Page(
        params PresentedResourceLiteralField[] literals) => new(
        "resource-1",
        new PresentedProjectResourcePortrait(
            "resource-1",
            "http://fogid.net/o/photo-doc",
            "Фотодокумент",
            literals,
            Array.Empty<PresentedResourceDirectLink>(),
            Array.Empty<PresentedResourceInverseLink>(),
            Provenance: null),
        Array.Empty<SemanticPhotoCard>(),
        Array.Empty<SemanticResourceLink>(),
        Array.Empty<SemanticResourceLink>(),
        Array.Empty<SemanticResourceLink>(),
        Array.Empty<SemanticResourceLink>());
}
