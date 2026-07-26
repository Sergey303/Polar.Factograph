using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

public sealed record CassetteAccessSnapshot(
    string CassetteId,
    bool Enabled,
    bool AllowWrite,
    IReadOnlySet<string> Rights)
{
    public bool CanRead => Rights.Contains(CassetteRights.Read);

    public bool Has(string right)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(right);
        return Rights.Contains(right);
    }
}

public sealed record ProjectAccessSnapshot(
    string UserId,
    bool IsMember,
    IReadOnlySet<string> ProjectRights,
    IReadOnlyDictionary<string, CassetteAccessSnapshot> Cassettes,
    string? DefaultWriteCassetteId)
{
    public bool HasProjectRight(string right)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(right);
        return ProjectRights.Contains(right);
    }

    public IReadOnlySet<string> ReadableCassetteIds => Cassettes.Values
        .Where(cassette => cassette.Enabled && cassette.CanRead)
        .Select(cassette => cassette.CassetteId)
        .ToHashSet(StringComparer.Ordinal);

    public bool HasCassetteRight(string cassetteId, string right)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cassetteId);
        ArgumentException.ThrowIfNullOrWhiteSpace(right);
        return Cassettes.TryGetValue(cassetteId, out CassetteAccessSnapshot? cassette) &&
               cassette.Enabled &&
               cassette.Has(right);
    }
}
