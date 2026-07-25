using Microsoft.AspNetCore.StaticFiles;

namespace Polar.Factograph.Api.Documents;

public sealed class DocumentContentTypeResolver
{
    private readonly FileExtensionContentTypeProvider _provider = new();

    public string Resolve(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return _provider.TryGetContentType(path, out string? contentType)
            ? contentType
            : "application/octet-stream";
    }
}
