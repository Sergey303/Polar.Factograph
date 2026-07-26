namespace Polar.Factograph.Fog;

internal static class CassettePreviewQueueFiles
{
    public static IEnumerable<string> EnumerateQueued(CassettePreviewQueuePaths paths) =>
        Directory.EnumerateFiles(paths.Queued, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal);

    public static bool TryClaim(
        string queuedPath,
        CassettePreviewQueuePaths paths,
        out string processingPath)
    {
        processingPath = Path.Combine(paths.Processing, Path.GetFileName(queuedPath));
        try
        {
            File.Move(queuedPath, processingPath);
            File.SetLastWriteTimeUtc(processingPath, DateTime.UtcNow);
            return true;
        }
        catch (IOException) when (!File.Exists(queuedPath) || File.Exists(processingPath))
        {
            return false;
        }
    }

    public static void RecoverStale(
        CassettePreviewQueuePaths paths,
        TimeSpan leaseTimeout)
    {
        DateTime cutoff = DateTime.UtcNow.Subtract(leaseTimeout);
        foreach (string processingPath in Directory.EnumerateFiles(
                     paths.Processing,
                     "*.json",
                     SearchOption.TopDirectoryOnly))
        {
            if (File.GetLastWriteTimeUtc(processingPath) > cutoff)
            {
                continue;
            }

            string queuedPath = Path.Combine(paths.Queued, Path.GetFileName(processingPath));
            if (File.Exists(queuedPath))
            {
                MoveToFailed(processingPath, paths, "stale-duplicate");
                continue;
            }

            File.Move(processingPath, queuedPath);
        }
    }

    public static void MoveBack(string processingPath, CassettePreviewQueuePaths paths) =>
        File.Move(
            processingPath,
            Path.Combine(paths.Queued, Path.GetFileName(processingPath)));

    public static string MoveToFailed(
        string processingPath,
        CassettePreviewQueuePaths paths,
        string suffix = "failed")
    {
        string sourceName = Path.GetFileNameWithoutExtension(processingPath);
        string targetPath = Path.Combine(
            paths.Failed,
            $"{sourceName}.{suffix}.{Guid.NewGuid():N}.json");
        File.Move(processingPath, targetPath);
        return targetPath;
    }

    public static void WriteFailureNote(string failedPath, string error)
    {
        try
        {
            File.WriteAllText($"{failedPath}.error.txt", error);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}