using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Documents;

public enum DocumentVariant
{
    Original = 1,
    Small = 2,
    Medium = 3,
    Normal = 4,
    Icon = 5
}

public static class DocumentVariantSelector
{
    public static DocumentVariant Parse(string value) => value.ToLowerInvariant() switch
    {
        "original" => DocumentVariant.Original,
        "icon" => DocumentVariant.Icon,
        "small" => DocumentVariant.Small,
        "medium" => DocumentVariant.Medium,
        "normal" => DocumentVariant.Normal,
        _ => throw new ArgumentException(
            "Document variant must be original, icon, small, medium, or normal.",
            nameof(value))
    };

    public static string? Select(
        CassetteDocumentLocation location,
        DocumentVariant variant)
    {
        ArgumentNullException.ThrowIfNull(location);

        return variant switch
        {
            DocumentVariant.Original => location.OriginalPath,
            DocumentVariant.Icon => FirstAvailable(
                location.IconPreviewPath,
                location.SmallPreviewPath,
                location.MediumPreviewPath,
                location.NormalPreviewPath,
                location.OriginalPath),
            DocumentVariant.Small => FirstAvailable(
                location.SmallPreviewPath,
                location.IconPreviewPath,
                location.MediumPreviewPath,
                location.NormalPreviewPath,
                location.OriginalPath),
            DocumentVariant.Medium => FirstAvailable(
                location.MediumPreviewPath,
                location.NormalPreviewPath,
                location.SmallPreviewPath,
                location.IconPreviewPath,
                location.OriginalPath),
            DocumentVariant.Normal => FirstAvailable(
                location.NormalPreviewPath,
                location.MediumPreviewPath,
                location.SmallPreviewPath,
                location.IconPreviewPath,
                location.OriginalPath),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
        };
    }

    private static string? FirstAvailable(params string?[] paths) =>
        paths.FirstOrDefault(path => path is not null);
}
