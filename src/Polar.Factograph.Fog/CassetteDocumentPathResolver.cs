using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

public sealed record CassetteDocumentLocation(
    string CassetteId,
    string CassetteName,
    string DocumentUri,
    string FolderName,
    string DocumentNumber,
    string? OriginalPath,
    string? SmallPreviewPath,
    string? MediumPreviewPath,
    string? NormalPreviewPath)
{
    public string? IconPreviewPath { get; init; }
}

public sealed class CassetteDocumentPathResolver
{
    public static bool IsDocumentUri(string? documentUri)
    {
        if (string.IsNullOrWhiteSpace(documentUri) ||
            !Uri.TryCreate(documentUri, UriKind.Absolute, out Uri? uri) ||
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

    public CassetteDocumentLocation Resolve(
        ProjectDefinition project,
        string documentUri)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentUri);

        if (!Uri.TryCreate(documentUri, UriKind.Absolute, out Uri? uri) ||
            !string.Equals(uri.Scheme, "iiss", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Document URI must use the iiss scheme: {documentUri}");
        }

        string cassetteName = Uri.UnescapeDataString(uri.UserInfo);
        if (string.IsNullOrWhiteSpace(cassetteName))
        {
            throw new InvalidDataException($"Document URI has no cassette name: {documentUri}");
        }

        CassetteDefinition cassette = project.Cassettes.FirstOrDefault(candidate =>
            candidate.Enabled &&
            string.Equals(candidate.Name, cassetteName, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException(
                $"Document cassette '{cassetteName}' is not enabled in project '{project.ProjectId}'.");

        string path = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
        string[] segments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length < 2)
        {
            throw new InvalidDataException(
                $"Document URI must end with a four-character folder and document number: {documentUri}");
        }

        string folderName = segments[^2];
        string documentNumber = segments[^1];
        ValidatePathPart(folderName, "folder", documentUri);
        ValidatePathPart(documentNumber, "document number", documentUri);

        string cassettePath = Path.GetFullPath(cassette.Path);
        string originalPath = FindSingleDocumentFile(
            Path.Combine(cassettePath, "originals", folderName),
            documentNumber,
            documentUri,
            "original");
        string iconPreviewPath = FindSingleDocumentFile(
            Path.Combine(cassettePath, "documents", "icon", folderName),
            documentNumber,
            documentUri,
            "icon preview");
        string smallPreviewPath = FindSingleDocumentFile(
            Path.Combine(cassettePath, "documents", "small", folderName),
            documentNumber,
            documentUri,
            "small preview");
        string mediumPreviewPath = FindSingleDocumentFile(
            Path.Combine(cassettePath, "documents", "medium", folderName),
            documentNumber,
            documentUri,
            "medium preview");
        string normalPreviewPath = FindSingleDocumentFile(
            Path.Combine(cassettePath, "documents", "normal", folderName),
            documentNumber,
            documentUri,
            "normal preview");

        string? original = NullWhenMissing(originalPath);
        return new CassetteDocumentLocation(
            cassette.Id,
            cassette.Name,
            documentUri,
            folderName,
            documentNumber,
            original,
            CurrentPreviewOrNull(smallPreviewPath, original),
            CurrentPreviewOrNull(mediumPreviewPath, original),
            CurrentPreviewOrNull(normalPreviewPath, original))
        {
            IconPreviewPath = CurrentPreviewOrNull(iconPreviewPath, original)
        };
    }

    private static void ValidatePathPart(
        string value,
        string description,
        string documentUri)
    {
        if (!IsSafeFourCharacterPart(value))
        {
            throw new InvalidDataException(
                $"Document URI {description} must be one safe four-character value: {documentUri}");
        }
    }

    private static bool IsSafeFourCharacterPart(string value) =>
        value.Length == 4 &&
        value is not "." and not ".." &&
        value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
        !value.Contains(Path.DirectorySeparatorChar) &&
        !value.Contains(Path.AltDirectorySeparatorChar);

    private static string FindSingleDocumentFile(
        string directory,
        string documentNumber,
        string documentUri,
        string kind)
    {
        if (!Directory.Exists(directory))
        {
            return string.Empty;
        }

        string[] candidates = Directory
            .EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
            .Where(path =>
            {
                string fileName = Path.GetFileName(path);
                return string.Equals(fileName, documentNumber, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(
                           Path.GetFileNameWithoutExtension(fileName),
                           documentNumber,
                           StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return candidates.Length switch
        {
            0 => string.Empty,
            1 => Path.GetFullPath(candidates[0]),
            _ => throw new InvalidDataException(
                $"Document URI has multiple {kind} files in '{directory}': {documentUri}")
        };
    }

    private static string? CurrentPreviewOrNull(string previewPath, string? originalPath)
    {
        string? preview = NullWhenMissing(previewPath);
        if (preview is null || originalPath is null)
        {
            return preview;
        }

        return File.GetLastWriteTimeUtc(preview) >= File.GetLastWriteTimeUtc(originalPath)
            ? preview
            : null;
    }

    private static string? NullWhenMissing(string value) =>
        string.IsNullOrEmpty(value) ? null : value;
}
