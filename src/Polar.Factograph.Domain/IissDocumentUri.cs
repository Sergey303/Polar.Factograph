namespace Polar.Factograph.Domain;

public static class IissDocumentUri
{
    public static bool IsPhysicalDocument(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) ||
            !string.Equals(uri.Scheme, "iiss", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(Uri.UnescapeDataString(uri.UserInfo)))
        {
            return false;
        }

        string path = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
        string[] segments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return segments.Length >= 2 &&
               IsSafeFourCharacterPart(segments[^2]) &&
               IsSafeFourCharacterPart(segments[^1]);
    }

    public static bool IsSafeFourCharacterPart(string value) =>
        value.Length == 4 &&
        value is not "." and not ".." &&
        value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
        !value.Contains(Path.DirectorySeparatorChar) &&
        !value.Contains(Path.AltDirectorySeparatorChar);
}
