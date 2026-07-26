namespace Polar.Factograph.Fog;

internal static class FogAtomicFileCommitter
{
    public static void Commit(string temporaryPath, string targetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);

        File.Move(temporaryPath, targetPath, overwrite: true);
    }

    public static void DeleteTemporary(string temporaryPath)
    {
        if (File.Exists(temporaryPath))
        {
            File.Delete(temporaryPath);
        }
    }
}
