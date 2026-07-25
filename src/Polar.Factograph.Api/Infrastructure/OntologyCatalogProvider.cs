using System.Collections.Concurrent;
using Polar.Factograph.Application;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class OntologyCatalogProvider(XmlOntologyCatalogLoader loader)
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public async Task<OntologyCatalog> GetAsync(
        string ontologyPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ontologyPath);

        string fullPath = Path.GetFullPath(ontologyPath);
        DateTime modifiedAtUtc = File.GetLastWriteTimeUtc(fullPath);
        if (_cache.TryGetValue(fullPath, out CacheEntry? cached) &&
            cached.ModifiedAtUtc == modifiedAtUtc)
        {
            return cached.Catalog;
        }

        OntologyCatalog catalog = await loader.LoadAsync(fullPath, cancellationToken);
        _cache[fullPath] = new CacheEntry(modifiedAtUtc, catalog);
        return catalog;
    }

    private sealed record CacheEntry(DateTime ModifiedAtUtc, OntologyCatalog Catalog);
}
