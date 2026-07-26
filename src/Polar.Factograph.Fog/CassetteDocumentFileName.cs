namespace Polar.Factograph.Fog;

internal static class CassetteDocumentFileName
{
    private const int MaxExtensionLength = 16;

    public static string Extension(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        if (!string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal))
        {
            throw new ArgumentException("Document file name must not contain a path.", nameof(fileName));
        }

        string extension = Path.GetExtension(fileName);
        if (extension.Length is < 2 or > MaxExtensionLength + 1 ||
            extension[1..].Any(character => !char.IsAsciiLetterOrDigit(character)))
        {
            throw new ArgumentException(
                $"Document file extension must contain 1-{MaxExtensionLength} ASCII letters or digits.",
                nameof(fileName));
        }

        return extension.ToLowerInvariant();
    }

    public static void RequireSameExtension(string fileName, string existingPath)
    {
        string requested = Extension(fileName);
        string existing = Path.GetExtension(existingPath);
        if (!string.Equals(requested, existing, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Replacement extension '{requested}' must match existing extension '{existing}'.",
                nameof(fileName));
        }
    }
}
