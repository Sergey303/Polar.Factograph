using System.Text.Json;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

public sealed class FileSystemCassettePreviewRequestWriter : ICassettePreviewRequestWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<CassettePreviewQueueResult> QueueAsync(
        CassetteDefinition cassette,
        CassetteDocumentWriteResult document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cassette);
        ArgumentNullException.ThrowIfNull(document);
        if (!string.Equals(cassette.Id, document.CassetteId, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Document cassette '{document.CassetteId}' does not match '{cassette.Id}'.");
        }

        string? temporaryPath = null;
        try
        {
            string directory = Path.Combine(
                Path.GetFullPath(cassette.Path), "documents", "preview-queue");
            Directory.CreateDirectory(directory);
            DateTimeOffset queuedAtUtc = DateTimeOffset.UtcNow;
            string requestId = Guid.NewGuid().ToString("N");
            string fileName = $"{document.FolderName}-{document.DocumentNumber}-{requestId}.json";
            string finalPath = Path.Combine(directory, fileName);
            temporaryPath = Path.Combine(directory, $".{fileName}.tmp");
            CassettePreviewRequest request = new(
                requestId, queuedAtUtc, document.CassetteId, document.CassetteName,
                document.DocumentUri, document.FolderName, document.DocumentNumber,
                document.FileName, document.Length, document.Sha256, document.Replaced);

            await using (FileStream stream = new(
                temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                16 * 1024, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(
                    stream, request, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, finalPath);
            temporaryPath = null;
            return CassettePreviewQueueResult.Queued(requestId, queuedAtUtc);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            return CassettePreviewQueueResult.Failed();
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    private static void TryDelete(string? path)
    {
        try
        {
            if (path is not null)
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}