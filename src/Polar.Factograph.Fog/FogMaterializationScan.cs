namespace Polar.Factograph.Fog;

internal sealed class FogMaterializationScan
{
    private readonly Dictionary<string, string?> _substitutions = new(StringComparer.Ordinal);
    private readonly HashSet<string> _resourceIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _duplicateIds = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, string?> Substitutions => _substitutions;
    public IReadOnlySet<string> DuplicateIds => _duplicateIds;
    public long SourceRecords { get; private set; }
    public long ResourceDefinitions { get; private set; }
    public long DeleteOperations { get; private set; }
    public long SubstituteOperations { get; private set; }

    public bool ContainsResource(string resourceId) => _resourceIds.Contains(resourceId);

    public void Add(FogSourceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        SourceRecords++;

        switch (record.Kind)
        {
            case FogRecordKind.Delete:
                DeleteOperations++;
                _substitutions[record.ResourceId] = null;
                break;

            case FogRecordKind.Substitute:
                SubstituteOperations++;
                AddSubstitution(record);
                break;

            case FogRecordKind.Resource:
                ResourceDefinitions++;
                if (!_resourceIds.Add(record.ResourceId))
                {
                    _duplicateIds.Add(record.ResourceId);
                }
                break;

            default:
                throw new InvalidDataException($"Unknown Fog record kind: {record.Kind}.");
        }
    }

    private void AddSubstitution(FogSourceRecord record)
    {
        if (_substitutions.TryGetValue(record.ResourceId, out string? existing) && existing is null)
        {
            return;
        }

        _substitutions[record.ResourceId] = record.SubstituteTargetId
            ?? throw new InvalidDataException(
                $"Substitute target is absent for '{record.ResourceId}'.");
    }
}
