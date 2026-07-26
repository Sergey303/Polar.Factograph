using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Previews;

internal sealed class PreviewOutputPlan : IDisposable
{
    private PreviewOutputPlan(string originalPath, PreviewOutputFile[] outputs)
    {
        OriginalPath = originalPath;
        Outputs = outputs;
    }

    public string OriginalPath { get; }
    public IReadOnlyList<PreviewOutputFile> Outputs { get; }

    public static PreviewOutputPlan Create(
        CassetteDefinition cassette,
        CassettePreviewRequest request,
        PreviewWorkerOptions options)
    {
        string root = Path.GetFullPath(cassette.Path);
        string originalDirectory = Path.Combine(root, "originals", request.FolderName);
        string originalPath = Path.GetFullPath(
            Path.Combine(originalDirectory, request.OriginalFileName));
        RequireInside(originalDirectory, originalPath);
        if (!File.Exists(originalPath))
        {
            throw new PreviewRenderingException("The original document is missing.", false);
        }

        PreviewOutputFile[] outputs = [
            CreateOutput(root, "small", request, options.SmallWidth, options.OutputExtension),
            CreateOutput(root, "medium", request, options.MediumWidth, options.OutputExtension),
            CreateOutput(root, "normal", request, options.NormalWidth, options.OutputExtension)
        ];
        return new PreviewOutputPlan(originalPath, outputs);
    }

    public void Publish()
    {
        foreach (PreviewOutputFile output in Outputs)
        {
            File.Move(output.TemporaryPath, output.FinalPath, overwrite: true);
        }
    }

    public void Dispose()
    {
        foreach (PreviewOutputFile output in Outputs)
        {
            TryDelete(output.TemporaryPath);
        }
    }

    private static PreviewOutputFile CreateOutput(
        string root,
        string variant,
        CassettePreviewRequest request,
        int width,
        string defaultExtension)
    {
        string directory = Path.Combine(root, "documents", variant, request.FolderName);
        Directory.CreateDirectory(directory);
        string[] existing = Directory.EnumerateFiles(directory)
            .Where(path => string.Equals(
                Path.GetFileNameWithoutExtension(path),
                request.DocumentNumber,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (existing.Length > 1)
        {
            throw new PreviewRenderingException(
                $"The {variant} preview has multiple current files.", false);
        }

        string extension = existing.Length == 1
            ? Path.GetExtension(existing[0])
            : $".{defaultExtension.ToLowerInvariant()}";
        string finalPath = existing.SingleOrDefault() ??
            Path.Combine(directory, request.DocumentNumber + extension);
        string temporaryPath = Path.Combine(
            directory,
            $".{request.DocumentNumber}.{request.RequestId}.{Guid.NewGuid():N}.tmp{extension}");
        return new PreviewOutputFile(variant, width, finalPath, temporaryPath);
    }

    private static void RequireInside(string directory, string path)
    {
        string prefix = Path.GetFullPath(directory) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new PreviewRenderingException("The original path is invalid.", false);
        }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
    }
}

internal sealed record PreviewOutputFile(
    string Variant,
    int Width,
    string FinalPath,
    string TemporaryPath);
