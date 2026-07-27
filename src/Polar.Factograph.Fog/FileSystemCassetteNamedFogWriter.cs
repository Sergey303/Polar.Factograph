using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

public sealed class FileSystemCassetteNamedFogWriter : ICassetteNamedFogWriter
{
    public async Task<CassetteDocumentWriteResult> AddAsync(
        CassetteDefinition cassette,
        Stream content,
        string fileName,
        long maxBytes,
        CancellationToken cancellationToken = default)
    {
        CassetteDocumentWriterRules.RequireWritable(cassette);
        RequireFogFileName(fileName);
        await using CassetteDocumentWriteLease lease =
            CassetteDocumentWriteLease.Acquire(cassette.Path);
        CassetteDocumentSlot slot = CassetteDocumentSlotAllocator.Allocate(
            cassette,
            ".fog");
        string directory = Path.GetDirectoryName(slot.Path)
            ?? throw new InvalidDataException($"Fog target has no directory: {slot.Path}");
        string targetPath = Path.Combine(
            directory,
            $"{slot.DocumentNumber}-{fileName}");
        CassetteDocumentCopyResult copy = await CassetteDocumentAtomicFileWriter.WriteAsync(
            content,
            targetPath,
            maxBytes,
            overwrite: false,
            cancellationToken);
        string documentUri = CassetteDocumentUri.Build(
            cassette,
            slot.FolderName,
            slot.DocumentNumber);
        return CassetteDocumentWriteResultFactory.Create(
            cassette,
            documentUri,
            slot.FolderName,
            slot.DocumentNumber,
            targetPath,
            copy,
            replaced: false);
    }

    private static void RequireFogFileName(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        if (!string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal) ||
            !string.Equals(Path.GetExtension(fileName), ".fog", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "A named Fog must use a plain .fog filename without a directory.",
                nameof(fileName));
        }
    }
}
