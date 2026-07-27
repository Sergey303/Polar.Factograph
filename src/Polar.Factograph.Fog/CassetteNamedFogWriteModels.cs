using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

public interface ICassetteNamedFogWriter
{
    Task<CassetteDocumentWriteResult> AddAsync(
        CassetteDefinition cassette,
        Stream content,
        string fileName,
        long maxBytes,
        CancellationToken cancellationToken = default);
}
