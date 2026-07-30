using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Documents;

public sealed record DocumentImageSelection(string Path, string Variant);

public static class DocumentImageSelector
{
    public static DocumentImageSelection? Select(
        CassetteDocumentLocation location,
        DocumentContentTypeResolver contentTypes)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(contentTypes);

        (string Variant, string? Path)[] candidates =
        [
            ("normal", location.NormalPreviewPath),
            ("medium", location.MediumPreviewPath),
            ("small", location.SmallPreviewPath),
            ("icon", location.IconPreviewPath),
            ("original", location.OriginalPath)
        ];
        foreach ((string variant, string? path) in candidates)
        {
            if (path is not null &&
                contentTypes.Resolve(path).StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return new DocumentImageSelection(path, variant);
            }
        }

        return null;
    }
}
