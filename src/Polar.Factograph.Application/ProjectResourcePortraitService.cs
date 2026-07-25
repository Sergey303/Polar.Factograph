using Polar.Factograph.Storage;

namespace Polar.Factograph.Application;

public sealed record ResourceLiteralField(
    string Predicate,
    string Value,
    string? Language,
    string? DataType);

public sealed record ResourceDirectLink(
    string Predicate,
    string TargetResourceId);

public sealed record ResourceInverseLink(
    string Predicate,
    string SourceResourceId,
    string SourceCassetteId);

public sealed record ResourceProvenance(
    Guid SourceRecordId,
    string SourceCassetteId,
    string SourceFogPath,
    DateTimeOffset ModifiedAt);

public sealed record ProjectResourcePortrait(
    string ResourceId,
    string? Type,
    IReadOnlyList<ResourceLiteralField> Literals,
    IReadOnlyList<ResourceDirectLink> DirectLinks,
    IReadOnlyList<ResourceInverseLink> InverseLinks,
    ResourceProvenance Provenance);

public sealed class ProjectResourcePortraitService
{
    private const string RdfType = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
    private const string SystemCassetteId = "$system";
    private readonly IProjectRdfStore _store;

    public ProjectResourcePortraitService(IProjectRdfStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async ValueTask<ProjectResourcePortrait?> GetAsync(
        string resourceId,
        IReadOnlySet<string> allowedCassetteIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        ArgumentNullException.ThrowIfNull(allowedCassetteIds);

        ResourceHead? head = await _store.GetResourceHeadAsync(resourceId, cancellationToken);
        if (head is null ||
            head.IsDeleted ||
            (!allowedCassetteIds.Contains(head.SourceCassetteId) &&
             !string.Equals(head.SourceCassetteId, SystemCassetteId, StringComparison.Ordinal)))
        {
            return null;
        }

        HashSet<string> effectiveCassetteIds = new(allowedCassetteIds, StringComparer.Ordinal)
        {
            SystemCassetteId
        };

        List<TripleRow> outgoing = await ReadAllAsync(
            _store.FindAsync(
                new TriplePattern(Subject: resourceId),
                effectiveCassetteIds,
                cancellationToken),
            cancellationToken);

        List<TripleRow> incoming = await ReadAllAsync(
            _store.FindAsync(
                new TriplePattern(
                    ObjectKind: TripleObjectKind.Iri,
                    ObjectValue: resourceId),
                effectiveCassetteIds,
                cancellationToken),
            cancellationToken);

        string? type = outgoing
            .Where(triple =>
                triple.ObjectKind == TripleObjectKind.Iri &&
                string.Equals(triple.Predicate, RdfType, StringComparison.Ordinal))
            .Select(triple => triple.ObjectValue)
            .OrderBy(value => value, StringComparer.Ordinal)
            .FirstOrDefault();

        ResourceLiteralField[] literals = outgoing
            .Where(triple => triple.ObjectKind == TripleObjectKind.Literal)
            .OrderBy(triple => triple.Predicate, StringComparer.Ordinal)
            .ThenBy(triple => triple.Language ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(triple => triple.ObjectValue, StringComparer.Ordinal)
            .ThenBy(triple => triple.TripleId)
            .Select(triple => new ResourceLiteralField(
                triple.Predicate,
                triple.ObjectValue,
                triple.Language,
                triple.DataType))
            .ToArray();

        ResourceDirectLink[] directLinks = outgoing
            .Where(triple =>
                triple.ObjectKind == TripleObjectKind.Iri &&
                !string.Equals(triple.Predicate, RdfType, StringComparison.Ordinal))
            .OrderBy(triple => triple.Predicate, StringComparer.Ordinal)
            .ThenBy(triple => triple.ObjectValue, StringComparer.Ordinal)
            .ThenBy(triple => triple.TripleId)
            .Select(triple => new ResourceDirectLink(
                triple.Predicate,
                triple.ObjectValue))
            .ToArray();

        ResourceInverseLink[] inverseLinks = incoming
            .OrderBy(triple => triple.Predicate, StringComparer.Ordinal)
            .ThenBy(triple => triple.Subject, StringComparer.Ordinal)
            .ThenBy(triple => triple.TripleId)
            .Select(triple => new ResourceInverseLink(
                triple.Predicate,
                triple.Subject,
                triple.SourceCassetteId))
            .ToArray();

        return new ProjectResourcePortrait(
            resourceId,
            type,
            literals,
            directLinks,
            inverseLinks,
            new ResourceProvenance(
                head.CurrentSourceRecordId,
                head.SourceCassetteId,
                head.SourceFogPath,
                head.ModifiedAt));
    }

    private static async Task<List<TripleRow>> ReadAllAsync(
        IAsyncEnumerable<TripleRow> source,
        CancellationToken cancellationToken)
    {
        List<TripleRow> result = new();
        await foreach (TripleRow triple in source.WithCancellation(cancellationToken))
        {
            result.Add(triple);
        }

        return result;
    }
}
