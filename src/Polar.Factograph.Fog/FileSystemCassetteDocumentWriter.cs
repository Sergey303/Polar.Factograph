using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

public sealed class FileSystemCassetteDocumentWriter : ICassetteDocumentWriter
{
    public async Task<CassetteDocumentWriteResult> AddAsync(
        CassetteDefinition cassette,
        Stream content,
        string fileName,
        long maxBytes,
        CancellationToken cancellationToken = default)
    {
        RequireWritable(cassette);
        string extension = CassetteDocumentFileName.Extension(fileName);
        await using CassetteDocumentWriteLease lease =
            CassetteDocumentWriteLease.Acquire(cassette.Path);
        CassetteDocumentSlot slot = CassetteDocumentSlotAllocator.Allocate(
            cassette,
            extension);
        CassetteDocumentCopyResult copy = await CassetteDocumentAtomicFileWriter.WriteAsync(
            content,
            slot.Path,
            maxBytes,
            overwrite: false,
            cancellationToken);
        return Result(
            cassette,
            CassetteDocumentUri.Build(cassette, slot.FolderName, slot.DocumentNumber),
            slot.FolderName,
            slot.DocumentNumber,
            slot.Path,
            copy,
            replaced: false);
    }

    public async Task<CassetteDocumentWriteResult> ReplaceAsync(
        CassetteDefinition cassette,
        CassetteDocumentLocation location,
        Stream content,
        string fileName,
        long maxBytes,
        CancellationToken cancellationToken = default)
    {
        RequireWritable(cassette);
        ArgumentNullException.ThrowIfNull(location);
        if (!string.Equals(cassette.Id, location.CassetteId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Document location belongs to another cassette.", nameof(location));
        }

        string originalPath = location.OriginalPath
            ?? throw new KeyNotFoundException($"Document original was not found: {location.DocumentUri}");
        CassetteDocumentFileName.RequireSameExtension(fileName, originalPath);
        await using CassetteDocumentWriteLease lease =
            CassetteDocumentWriteLease.Acquire(cassette.Path);
        if (!File.Exists(originalPath))
        {
            throw new KeyNotFoundException($"Document original was not found: {location.DocumentUri}");
        }

        CassetteDocumentCopyResult copy = await CassetteDocumentAtomicFileWriter.WriteAsync(
            content,
            originalPath,
            maxBytes,
            overwrite: true,
            cancellationToken);
        return Result(
            cassette,
            location.DocumentUri,
            location.FolderName,
            location.DocumentNumber,
            originalPath,
            copy,
            replaced: true);
    }

    private static void RequireWritable(CassetteDefinition cassette)
    {
        ArgumentNullException.ThrowIfNull(cassette);
        if (!cassette.Enabled || !cassette.AllowWrite)
        {
            throw new InvalidOperationException($"Cassette is not writable: {cassette.Id}");
        }
    }

    private static CassetteDocumentWriteResult Result(
        CassetteDefinition cassette,
        string documentUri,
        string folderName,
        string documentNumber,
        string path,
        CassetteDocumentCopyResult copy,
        bool replaced) => new(
            cassette.Id,
            cassette.Name,
            documentUri,
            folderName,
            documentNumber,
            Path.GetFileName(path),
            copy.Length,
            copy.Sha256,
            replaced);
}
