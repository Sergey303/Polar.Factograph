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
        DocumentVariant variant) => variant switch
    {
        DocumentVariant.Original => location.OriginalPath,
        DocumentVariant.Icon => location.IconPreviewPath ?? location.SmallPreviewPath,
        DocumentVariant.Small => location.SmallPreviewPath,
        DocumentVariant.Medium => location.MediumPreviewPath,
        DocumentVariant.Normal => location.NormalPreviewPath,
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
    };
}
