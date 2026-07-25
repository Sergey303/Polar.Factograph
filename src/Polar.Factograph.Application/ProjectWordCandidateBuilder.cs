using Polar.Factograph.Storage;

namespace Polar.Factograph.Application;

internal sealed class ProjectWordCandidateBuilder(string resourceId)
{
    private readonly HashSet<string> _matchedWords = new(StringComparer.Ordinal);
    private readonly HashSet<ProjectSearchEvidence> _matches = new();

    public string ResourceId { get; } = resourceId;

    public int Score => _matchedWords.Count;

    public IReadOnlyList<ProjectSearchEvidence> Matches => _matches
        .OrderBy(match => match.Predicate, StringComparer.Ordinal)
        .ThenBy(match => match.Value, StringComparer.Ordinal)
        .ToArray();

    public void Add(string word, WordSearchHit hit)
    {
        _matchedWords.Add(word);
        _matches.Add(ProjectSearchRules.ToEvidence(hit));
    }
}
