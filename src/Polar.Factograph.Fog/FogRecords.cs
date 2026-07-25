namespace Polar.Factograph.Fog;

public static class LegacyFogVocabulary
{
    public const string Namespace = "http://fogid.net/o/";
    public const string RdfNamespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    public const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
    public const string CassetteRootCollectionId = "cassetterootcollection";
}

public enum FogRecordKind
{
    Resource = 1,
    Delete = 2,
    Substitute = 3
}

public enum FogPropertyKind
{
    Literal = 1,
    Resource = 2
}

public readonly record struct FogRecordKey(string SourceFogPath, long SourceOrdinal);

public sealed record FogProperty(
    string Predicate,
    FogPropertyKind Kind,
    string Value,
    string? Language = null,
    string? DataType = null);

public sealed record FogSourceRecord(
    FogRecordKey Key,
    string SourceCassetteId,
    string SourceCassetteName,
    FogRecordKind Kind,
    string ResourceId,
    string? Type,
    string? SubstituteTargetId,
    DateTime ModifiedAt,
    string? ModifiedAtRaw,
    IReadOnlyList<FogProperty> Properties);

public sealed record FogCurrentRecord(
    string ResourceId,
    string Type,
    DateTime ModifiedAt,
    IReadOnlyList<FogProperty> Properties,
    string? SourceCassetteId,
    string? SourceCassetteName,
    string? SourceFogPath,
    long? SourceOrdinal,
    bool IsSynthetic);

public interface IFogRecordReader
{
    IAsyncEnumerable<FogSourceRecord> ReadAsync(
        FogSourceDescriptor source,
        CancellationToken cancellationToken = default);
}

public delegate IAsyncEnumerable<FogSourceRecord> FogRecordStreamFactory(
    CancellationToken cancellationToken);

public sealed record FogMaterializationStatistics(
    int SourceFiles,
    long SourceRecords,
    long ResourceDefinitions,
    long DeleteOperations,
    long SubstituteOperations,
    int DuplicateResourceIds,
    int RedirectedIds,
    int DeletedIds,
    long CurrentSourceResources,
    int SyntheticResources,
    long CurrentProperties);

public sealed class FogMaterializationPlan
{
    private readonly IReadOnlyDictionary<string, string?> _resolvedSubstitutions;
    private readonly IReadOnlySet<string> _duplicateIds;
    private readonly IReadOnlyDictionary<string, FogRecordKey> _winningDefinitions;

    internal FogMaterializationPlan(
        IReadOnlyDictionary<string, string?> resolvedSubstitutions,
        IReadOnlySet<string> duplicateIds,
        IReadOnlyDictionary<string, FogRecordKey> winningDefinitions,
        bool containsCurrentCassetteRoot,
        long sourceRecords,
        long resourceDefinitions,
        long deleteOperations,
        long substituteOperations)
    {
        _resolvedSubstitutions = resolvedSubstitutions;
        _duplicateIds = duplicateIds;
        _winningDefinitions = winningDefinitions;
        ContainsCurrentCassetteRoot = containsCurrentCassetteRoot;
        SourceRecords = sourceRecords;
        ResourceDefinitions = resourceDefinitions;
        DeleteOperations = deleteOperations;
        SubstituteOperations = substituteOperations;
    }

    public IReadOnlyDictionary<string, string?> ResolvedSubstitutions => _resolvedSubstitutions;
    public int DuplicateResourceIds => _duplicateIds.Count;
    public int RedirectedIds => _resolvedSubstitutions.Count(pair => pair.Value is not null);
    public int DeletedIds => _resolvedSubstitutions.Count(pair => pair.Value is null);
    public bool ContainsCurrentCassetteRoot { get; }
    public long SourceRecords { get; }
    public long ResourceDefinitions { get; }
    public long DeleteOperations { get; }
    public long SubstituteOperations { get; }

    public string ResolveReferencedId(string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);

        return _resolvedSubstitutions.TryGetValue(resourceId, out string? resolved) && resolved is not null
            ? resolved
            : resourceId;
    }

    internal bool Includes(FogSourceRecord record)
    {
        if (record.Kind != FogRecordKind.Resource)
        {
            return false;
        }

        if (_resolvedSubstitutions.ContainsKey(record.ResourceId))
        {
            return false;
        }

        return !_duplicateIds.Contains(record.ResourceId) ||
               (_winningDefinitions.TryGetValue(record.ResourceId, out FogRecordKey winner) &&
                winner == record.Key);
    }
}
