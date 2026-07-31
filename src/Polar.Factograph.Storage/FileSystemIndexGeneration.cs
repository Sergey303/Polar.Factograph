using System.Text;

namespace Polar.Factograph.Storage;

/// <summary>
/// Manages an index generation directory and atomically switches the CURRENT pointer after a successful build.
/// Orphaned completed generations are safe; incomplete staging directories are removed on abort or disposal.
/// </summary>
public sealed class FileSystemIndexGeneration : IAsyncDisposable
{
    private const string CurrentFileName = "CURRENT";
    private const int PointerPublishAttempts = 20;
    private readonly string _indexRoot;
    private bool _committed;
    private bool _aborted;

    private FileSystemIndexGeneration(
        string indexRoot,
        Guid generationId,
        string stagingPath,
        string finalPath)
    {
        _indexRoot = indexRoot;
        GenerationId = generationId;
        StagingPath = stagingPath;
        FinalPath = finalPath;
    }

    public Guid GenerationId { get; }

    public string StagingPath { get; }

    public string FinalPath { get; }

    public static FileSystemIndexGeneration Begin(string indexRoot, Guid? generationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexRoot);

        string fullRoot = Path.GetFullPath(indexRoot);
        Directory.CreateDirectory(fullRoot);

        Guid id = generationId ?? Guid.NewGuid();
        string generationName = GenerationDirectoryName(id);
        string stagingPath = Path.Combine(fullRoot, generationName + ".building");
        string finalPath = Path.Combine(fullRoot, generationName);

        if (Directory.Exists(stagingPath) || Directory.Exists(finalPath))
        {
            throw new IOException($"Index generation already exists: {id:N}");
        }

        Directory.CreateDirectory(stagingPath);
        return new FileSystemIndexGeneration(fullRoot, id, stagingPath, finalPath);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfFinished();
        cancellationToken.ThrowIfCancellationRequested();

        Directory.Move(StagingPath, FinalPath);

        string currentPath = Path.Combine(_indexRoot, CurrentFileName);
        string temporaryPath = Path.Combine(
            _indexRoot,
            $"{CurrentFileName}.{GenerationId:N}.tmp");

        try
        {
            byte[] content = Encoding.UTF8.GetBytes(Path.GetFileName(FinalPath) + Environment.NewLine);
            await using (FileStream stream = new(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 4 * 1024,
                             FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(content, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            await PublishCurrentPointerAsync(
                temporaryPath,
                currentPath,
                cancellationToken);
            _committed = true;
        }
        catch
        {
            TryDeleteDirectory(FinalPath);
            throw;
        }
        finally
        {
            TryDeleteFile(temporaryPath);
        }
    }

    public Task AbortAsync(CancellationToken cancellationToken = default)
    {
        if (_committed || _aborted)
        {
            return Task.CompletedTask;
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (Directory.Exists(StagingPath))
        {
            Directory.Delete(StagingPath, recursive: true);
        }

        _aborted = true;
        return Task.CompletedTask;
    }

    public static string? GetCurrentGenerationName(string indexRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexRoot);

        string fullRoot = Path.GetFullPath(indexRoot);
        string currentPath = Path.Combine(fullRoot, CurrentFileName);
        if (!File.Exists(currentPath))
        {
            return null;
        }

        string generationName = ReadAllTextShared(currentPath).Trim();
        if (string.IsNullOrWhiteSpace(generationName) ||
            !generationName.StartsWith("generation-", StringComparison.Ordinal) ||
            generationName.Contains(Path.DirectorySeparatorChar) ||
            generationName.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new InvalidDataException($"Invalid index CURRENT pointer: {currentPath}");
        }

        return generationName;
    }

    public static string? GetCurrentGenerationPath(string indexRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexRoot);

        string fullRoot = Path.GetFullPath(indexRoot);
        string? generationName = GetCurrentGenerationName(fullRoot);
        return generationName is null
            ? null
            : Path.Combine(fullRoot, generationName);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_committed && !_aborted)
        {
            await AbortAsync(CancellationToken.None);
        }
    }

    private static async Task PublishCurrentPointerAsync(
        string temporaryPath,
        string currentPath,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (File.Exists(currentPath))
                {
                    File.Replace(
                        temporaryPath,
                        currentPath,
                        destinationBackupFileName: null,
                        ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(temporaryPath, currentPath);
                }

                return;
            }
            catch (IOException) when (attempt < PointerPublishAttempts)
            {
                await DelayBeforeRetryAsync(attempt, cancellationToken);
            }
            catch (UnauthorizedAccessException) when (attempt < PointerPublishAttempts)
            {
                await DelayBeforeRetryAsync(attempt, cancellationToken);
            }
        }
    }

    private static Task DelayBeforeRetryAsync(
        int attempt,
        CancellationToken cancellationToken) =>
        Task.Delay(TimeSpan.FromMilliseconds(Math.Min(500, 25 * attempt)), cancellationToken);

    private static string ReadAllTextShared(string path)
    {
        using FileStream stream = new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using StreamReader reader = new(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Preserve the original publication failure.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Preserve the original publication failure. An orphan generation is not CURRENT.
        }
    }

    private static string GenerationDirectoryName(Guid generationId) =>
        $"generation-{generationId:N}";

    private void ThrowIfFinished()
    {
        if (_committed)
        {
            throw new InvalidOperationException("Index generation is already committed.");
        }

        if (_aborted)
        {
            throw new InvalidOperationException("Index generation is already aborted.");
        }
    }
}
