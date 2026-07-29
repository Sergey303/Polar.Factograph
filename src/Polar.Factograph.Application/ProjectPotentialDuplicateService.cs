using Polar.Factograph.Storage;

namespace Polar.Factograph.Application;

public sealed record PotentialDuplicateResource(
    string ResourceId,
    string DisplayName,
    string? Type,
    string? TypeLabel,
    string Predicate,
    string MatchedValue,
    bool AlternativeWriting);

public sealed class ProjectPotentialDuplicateService(
    IProjectRdfStore store,
    AuthorizedProjectReadService reads,
    OntologyCatalog ontology)
{
    private const string Alias = "http://fogid.net/o/alias";
    private static readonly IReadOnlySet<string> NonTextRanges = new HashSet<string>(
        [
            "http://fogid.net/o/boolean",
            "http://fogid.net/o/bool",
            "http://fogid.net/o/integer",
            "http://fogid.net/o/int",
            "http://fogid.net/o/long",
            "http://fogid.net/o/float",
            "http://fogid.net/o/double",
            "http://fogid.net/o/decimal",
            "http://fogid.net/o/date"
        ],
        StringComparer.Ordinal);

    public async Task<IReadOnlyList<PotentialDuplicateResource>> FindAsync(
        string typeId,
        string predicate,
        string value,
        ProjectAccessSnapshot access,
        int limit = 10,
        string preferredLanguage = "ru",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(predicate);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(access);
        ArgumentException.ThrowIfNullOrWhiteSpace(preferredLanguage);
        if (limit is < 1 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        string entered = value.Trim();
        if (entered.Length == 0 || entered.Length > 512 || !IsComparableTextProperty(predicate))
        {
            return Array.Empty<PotentialDuplicateResource>();
        }

        IReadOnlySet<string> cassetteIds = ProjectAuthorization.RequireSearch(access);
        Dictionary<string, Candidate> candidates = new(StringComparer.Ordinal);
        await AddExactMatchesAsync(
            candidates,
            predicate,
            entered,
            cassetteIds,
            cancellationToken);

        if (IsNamePredicate(predicate))
        {
            await AddNameMatchesAsync(
                candidates,
                predicate,
                entered,
                access,
                preferredLanguage,
                cancellationToken);
        }

        List<PotentialDuplicateResource> result = new(limit);
        foreach (Candidate candidate in candidates.Values
                     .OrderBy(candidate => candidate.AlternativeWriting)
                     .ThenBy(candidate => candidate.Value, StringComparer.CurrentCultureIgnoreCase)
                     .ThenBy(candidate => candidate.ResourceId, StringComparer.Ordinal))
        {
            ProjectResourcePortrait? portrait = await reads.GetPortraitAsync(
                candidate.ResourceId,
                access,
                cancellationToken);
            if (portrait is null || !TypeMatches(typeId, portrait.Type))
            {
                continue;
            }

            result.Add(new PotentialDuplicateResource(
                portrait.ResourceId,
                DisplayName(portrait, preferredLanguage),
                portrait.Type,
                portrait.Type is null
                    ? null
                    : ontology.LabelOf(portrait.Type, preferredLanguage) ?? portrait.Type,
                predicate,
                candidate.Value,
                candidate.AlternativeWriting));
            if (result.Count == limit)
            {
                break;
            }
        }

        return result;
    }

    private async Task AddExactMatchesAsync(
        IDictionary<string, Candidate> candidates,
        string predicate,
        string value,
        IReadOnlySet<string> cassetteIds,
        CancellationToken cancellationToken)
    {
        await foreach (TripleRow triple in store.FindAsync(
                           new TriplePattern(
                               Predicate: predicate,
                               ObjectKind: TripleObjectKind.Literal,
                               ObjectValue: value),
                           cassetteIds,
                           cancellationToken))
        {
            candidates.TryAdd(
                triple.Subject,
                new Candidate(triple.Subject, triple.ObjectValue, AlternativeWriting: false));
        }
    }

    private async Task AddNameMatchesAsync(
        IDictionary<string, Candidate> candidates,
        string predicate,
        string value,
        ProjectAccessSnapshot access,
        string preferredLanguage,
        CancellationToken cancellationToken)
    {
        HashSet<string> enteredVariants = ProjectNameQueryVariants.Create(value)
            .Select(variant => variant.Key)
            .ToHashSet(StringComparer.Ordinal);
        IReadOnlyList<ProjectResourceSearchResult> matches = await reads.SearchByNameAsync(
            value,
            access,
            limit: 50,
            preferredLanguage,
            cancellationToken);

        foreach (ProjectResourceSearchResult match in matches)
        {
            ProjectSearchEvidence? evidence = match.Matches.FirstOrDefault(item =>
                string.Equals(item.Predicate, predicate, StringComparison.Ordinal) &&
                ProjectNameQueryVariants.Create(item.Value)
                    .Any(variant => enteredVariants.Contains(variant.Key)));
            if (evidence is null)
            {
                continue;
            }

            bool alternative = !string.Equals(
                LegacySearchIndexProjector.NormalizeNameQuery(evidence.Value),
                LegacySearchIndexProjector.NormalizeNameQuery(value),
                StringComparison.Ordinal);
            if (!candidates.TryGetValue(match.ResourceId, out Candidate? existing) ||
                existing.AlternativeWriting && !alternative)
            {
                candidates[match.ResourceId] = new Candidate(
                    match.ResourceId,
                    evidence.Value,
                    alternative);
            }
        }
    }

    private bool IsComparableTextProperty(string predicate)
    {
        if (!ontology.TryGetTerm(predicate, out OntologyTerm? property) ||
            property is not { Kind: OntologyTermKind.DatatypeProperty } ||
            property.EnumerationStates.Count > 0)
        {
            return false;
        }

        return property.Ranges.Count == 0 ||
            property.Ranges.All(range => !NonTextRanges.Contains(range));
    }

    private bool TypeMatches(string requestedType, string? candidateType)
    {
        if (candidateType is null)
        {
            return false;
        }

        return string.Equals(candidateType, requestedType, StringComparison.Ordinal) ||
            ontology.AncestorsAndSelf(candidateType).Contains(requestedType, StringComparer.Ordinal);
    }

    private static bool IsNamePredicate(string predicate) =>
        string.Equals(predicate, SemanticBridgeVocabulary.Name, StringComparison.Ordinal) ||
        string.Equals(predicate, Alias, StringComparison.Ordinal);

    private static string DisplayName(ProjectResourcePortrait portrait, string preferredLanguage)
    {
        ResourceLiteralField? preferred = portrait.Literals.FirstOrDefault(field =>
            string.Equals(field.Predicate, SemanticBridgeVocabulary.Name, StringComparison.Ordinal) &&
            string.Equals(field.Language, preferredLanguage, StringComparison.OrdinalIgnoreCase));
        ResourceLiteralField? anyName = portrait.Literals.FirstOrDefault(field =>
            string.Equals(field.Predicate, SemanticBridgeVocabulary.Name, StringComparison.Ordinal));
        return preferred?.Value ?? anyName?.Value ?? portrait.ResourceId;
    }

    private sealed record Candidate(
        string ResourceId,
        string Value,
        bool AlternativeWriting);
}
