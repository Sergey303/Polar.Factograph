using Polar.Factograph.Storage;

namespace Polar.Factograph.Application.Tests;

internal sealed class CollectionSearchStoreStub(
    IReadOnlyDictionary<string, IReadOnlyList<NameSearchHit>> names)
    : IProjectSearchStore
{
    public Task<IReadOnlyList<NameSearchHit>> FindNamesByKeyAsync(
        string normalizedSearchKey,
        IReadOnlySet<string> allowedCassetteIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NameSearchHit>>(Array.Empty<NameSearchHit>());

    public Task<IReadOnlyList<NameSearchHit>> FindNamesByResourceAsync(
        string resourceId,
        IReadOnlySet<string> allowedCassetteIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<NameSearchHit> result = names.TryGetValue(
            resourceId,
            out IReadOnlyList<NameSearchHit>? values)
            ? values.Where(value => allowedCassetteIds.Contains(value.SourceCassetteId)).ToArray()
            : Array.Empty<NameSearchHit>();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<WordSearchHit>> FindWordAsync(
        string normalizedWord,
        IReadOnlySet<string> allowedCassetteIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WordSearchHit>>(Array.Empty<WordSearchHit>());
}
