namespace Polar.Factograph.Application;

public sealed record ProjectSearchEvidence(
    string Predicate,
    string Value,
    string? Language);

public sealed record ProjectResourceSearchResult(
    string ResourceId,
    string DisplayName,
    string? Type,
    string? TypeLabel,
    int Score,
    string SourceCassetteId,
    IReadOnlyList<ProjectSearchEvidence> Matches);

internal sealed record ProjectRankedCandidate(
    string ResourceId,
    string DisplayName,
    int Score,
    IReadOnlyList<ProjectSearchEvidence> Matches)
{
    public int Rank { get; init; } = Score;
}
