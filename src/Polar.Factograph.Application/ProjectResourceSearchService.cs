using Polar.Factograph.Storage;

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

/// <summary>
/// Executes legacy-compatible name and word searches over materialized exact-key indexes.
/// </summary>
public sealed class ProjectResourceSearchService
{
    private const string RdfType = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
    private const string SystemCassetteId = "$system";
    private readonly IProjectSearchStore _searchStore;
    private readonly IProjectRdfStore _rdfStore;
    private readonly OntologyCatalog? _ontology;

    public ProjectResourceSearchService(
        IProjectSearchStore searchStore,
        IProjectRdfStore rdfStore,
        OntologyCatalog? ontology = null)
    {
        _searchStore = searchStore ?? throw new ArgumentNullException(nameof(searchStore));
        _rdfStore = rdfStore ?? throw new ArgumentNullException(nameof(rdfStore));
        _ontology = ontology;
    }

    public async Task<IReadOnlyList<ProjectResourceSearchResult>> SearchByNameAsync(
        string query,
        IReadOnlySet<string> allowedCassetteIds,
        int limit = 50,
        string preferredLanguage = "ru",
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(query, allowedCassetteIds, limit, preferredLanguage);

        string searchKey = LegacySearchIndexProjector.NormalizeNameQuery(query);
        if (searchKey.Length == 0)
        {
            return Array.Empty<ProjectResourceSearchResult>();
        }

        HashSet<string> effectiveCassetteIds = EffectiveCassetteIds(allowedCassetteIds);
        IReadOnlyList<NameSearchHit> hits = await _searchStore.FindNamesByKeyAsync(
            searchKey,
            effectiveCassetteIds,
            cancellationToken);

        NameCandidate[] candidates = hits
            .GroupBy(hit => hit.ResourceId, StringComparer.Ordinal)
            .Select(group =>
            {
                NameSearchHit[] resourceHits = group
                    .Distinct()
                    .ToArray();
                return new NameCandidate(
                    group.Key,
                    resourceHits.Max(hit => NameScore(hit.Value, searchKey)),
                    SelectDisplayName(resourceHits, preferredLanguage, group.Key),
                    resourceHits
                        .Select(ToEvidence)
                        .OrderBy(evidence => evidence.Predicate, StringComparer.Ordinal)
                        .ThenBy(evidence => evidence.Value, StringComparer.Ordinal)
                        .ToArray());
            })
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.ResourceId, StringComparer.Ordinal)
            .ToArray();

        return await EnrichAsync(
            candidates.Select(candidate => new RankedCandidate(
                candidate.ResourceId,
                candidate.DisplayName,
                candidate.Score,
                candidate.Matches)),
            effectiveCassetteIds,
            limit,
            preferredLanguage,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectResourceSearchResult>> SearchByWordsAsync(
        string query,
        IReadOnlySet<string> allowedCassetteIds,
        int limit = 50,
        string preferredLanguage = "ru",
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(query, allowedCassetteIds, limit, preferredLanguage);

        IReadOnlyList<string> words = LegacySearchIndexProjector.NormalizeSearchWords(query);
        if (words.Count == 0)
        {
            return Array.Empty<ProjectResourceSearchResult>();
        }

        HashSet<string> effectiveCassetteIds = EffectiveCassetteIds(allowedCassetteIds);
        Dictionary<string, WordCandidateBuilder> candidates = new(StringComparer.Ordinal);

        foreach (string word in words)
        {
            IReadOnlyList<WordSearchHit> hits = await _searchStore.FindWordAsync(
                word,
                effectiveCassetteIds,
                cancellationToken);

            foreach (WordSearchHit hit in hits)
            {
                if (!candidates.TryGetValue(hit.ResourceId, out WordCandidateBuilder? candidate))
                {
                    candidate = new WordCandidateBuilder(hit.ResourceId);
                    candidates.Add(hit.ResourceId, candidate);
                }

                candidate.Add(word, hit);
            }
        }

        List<RankedCandidate> ranked = new();
        foreach (WordCandidateBuilder candidate in candidates.Values
                     .OrderByDescending(candidate => candidate.Score)
                     .ThenBy(candidate => candidate.ResourceId, StringComparer.Ordinal)
                     .Take(limit * 10))
        {
            IReadOnlyList<NameSearchHit> names = await _searchStore.FindNamesByResourceAsync(
                candidate.ResourceId,
                effectiveCassetteIds,
                cancellationToken);
            ranked.Add(new RankedCandidate(
                candidate.ResourceId,
                SelectDisplayName(names, preferredLanguage, candidate.ResourceId),
                candidate.Score,
                candidate.Matches));
        }

        return await EnrichAsync(
            ranked
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(candidate => candidate.ResourceId, StringComparer.Ordinal),
            effectiveCassetteIds,
            limit,
            preferredLanguage,
            cancellationToken);
    }

    private async Task<IReadOnlyList<ProjectResourceSearchResult>> EnrichAsync(
        IEnumerable<RankedCandidate> candidates,
        IReadOnlySet<string> effectiveCassetteIds,
        int limit,
        string preferredLanguage,
        CancellationToken cancellationToken)
    {
        List<ProjectResourceSearchResult> results = new(limit);

        foreach (RankedCandidate candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ResourceHead? head = await _rdfStore.GetResourceHeadAsync(
                candidate.ResourceId,
                cancellationToken);
            if (head is null || head.IsDeleted || !effectiveCassetteIds.Contains(head.SourceCassetteId))
            {
                continue;
            }

            string? type = await ReadTypeAsync(
                candidate.ResourceId,
                effectiveCassetteIds,
                cancellationToken);
            results.Add(new ProjectResourceSearchResult(
                candidate.ResourceId,
                candidate.DisplayName,
                type,
                type is null ? null : _ontology?.LabelOf(type, preferredLanguage) ?? type,
                candidate.Score,
                head.SourceCassetteId,
                candidate.Matches));

            if (results.Count == limit)
            {
                break;
            }
        }

        return results;
    }

    private async Task<string?> ReadTypeAsync(
        string resourceId,
        IReadOnlySet<string> effectiveCassetteIds,
        CancellationToken cancellationToken)
    {
        List<string> types = new();
        await foreach (TripleRow triple in _rdfStore.FindAsync(
                           new TriplePattern(
                               Subject: resourceId,
                               Predicate: RdfType,
                               ObjectKind: TripleObjectKind.Iri),
                           effectiveCassetteIds,
                           cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            types.Add(triple.ObjectValue);
        }

        return types.Order(StringComparer.Ordinal).FirstOrDefault();
    }

    private static void ValidateRequest(
        string query,
        IReadOnlySet<string> allowedCassetteIds,
        int limit,
        string preferredLanguage)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(allowedCassetteIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(preferredLanguage);
        if (limit is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Search limit must be between 1 and 500.");
        }
    }

    private static HashSet<string> EffectiveCassetteIds(IReadOnlySet<string> allowedCassetteIds) =>
        new(allowedCassetteIds, StringComparer.Ordinal)
        {
            SystemCassetteId
        };

    private static int NameScore(string value, string searchKey)
    {
        string normalized = LegacySearchIndexProjector.NormalizeNameQuery(value);
        if (string.Equals(normalized, searchKey, StringComparison.Ordinal))
        {
            return 3;
        }

        if (normalized.StartsWith(searchKey, StringComparison.Ordinal))
        {
            return 2;
        }

        return 1;
    }

    private static string SelectDisplayName(
        IReadOnlyList<NameSearchHit> names,
        string preferredLanguage,
        string fallbackResourceId)
    {
        NameSearchHit? selected = names
            .OrderBy(hit => string.Equals(
                hit.Predicate,
                LegacySearchIndexProjector.NamePredicate,
                StringComparison.Ordinal) ? 0 : 1)
            .ThenBy(hit => LanguagePriority(hit.Language, preferredLanguage))
            .ThenBy(hit => hit.Value, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return selected?.Value ?? fallbackResourceId;
    }

    private static int LanguagePriority(string? language, string preferredLanguage)
    {
        if (string.Equals(language, preferredLanguage, StringComparison.OrdinalIgnoreCase)) return 0;
        if (string.IsNullOrEmpty(language)) return 1;
        if (string.Equals(language, "ru", StringComparison.OrdinalIgnoreCase)) return 2;
        if (string.Equals(language, "en", StringComparison.OrdinalIgnoreCase)) return 3;
        return 4;
    }

    private static ProjectSearchEvidence ToEvidence(NameSearchHit hit) => new(
        hit.Predicate,
        hit.Value,
        hit.Language);

    private static ProjectSearchEvidence ToEvidence(WordSearchHit hit) => new(
        hit.Predicate,
        hit.Value,
        hit.Language);

    private sealed record NameCandidate(
        string ResourceId,
        int Score,
        string DisplayName,
        IReadOnlyList<ProjectSearchEvidence> Matches);

    private sealed record RankedCandidate(
        string ResourceId,
        string DisplayName,
        int Score,
        IReadOnlyList<ProjectSearchEvidence> Matches);

    private sealed class WordCandidateBuilder
    {
        private readonly HashSet<string> _matchedWords = new(StringComparer.Ordinal);
        private readonly HashSet<ProjectSearchEvidence> _matches = new();

        public WordCandidateBuilder(string resourceId)
        {
            ResourceId = resourceId;
        }

        public string ResourceId { get; }

        public int Score => _matchedWords.Count;

        public IReadOnlyList<ProjectSearchEvidence> Matches => _matches
            .OrderBy(match => match.Predicate, StringComparer.Ordinal)
            .ThenBy(match => match.Value, StringComparer.Ordinal)
            .ToArray();

        public void Add(string word, WordSearchHit hit)
        {
            _matchedWords.Add(word);
            _matches.Add(ToEvidence(hit));
        }
    }
}