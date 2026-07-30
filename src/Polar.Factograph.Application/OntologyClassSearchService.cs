using System.Text;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Application;

public sealed record OntologyClassSearchSuggestion(
    string ClassId,
    string Label,
    bool ExactMatch,
    bool IsAbstract);

public sealed record ProjectResourceTypeSearchPage(
    string ClassId,
    string Label,
    int Total,
    int Offset,
    int Limit,
    IReadOnlyList<ProjectResourceSearchResult> Results);

public sealed class OntologyClassSearchService(
    IProjectRdfStore rdfStore,
    IProjectSearchStore searchStore,
    OntologyCatalog ontology)
{
    private const string RdfType =
        "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
    private readonly ProjectResourceSummaryReader _summaries =
        new(rdfStore, searchStore, ontology);

    public IReadOnlyList<OntologyClassSearchSuggestion> Suggest(
        string query,
        int limit = 8,
        string preferredLanguage = "ru")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, 50);
        ArgumentException.ThrowIfNullOrWhiteSpace(preferredLanguage);

        string normalizedQuery = Normalize(query);
        if (normalizedQuery.Length < 2)
        {
            return Array.Empty<OntologyClassSearchSuggestion>();
        }

        return ontology.Terms
            .Where(term => term.Kind == OntologyTermKind.Class)
            .Select(term => Match(term, normalizedQuery, preferredLanguage))
            .Where(match => match is not null)
            .Select(match => match!)
            .OrderByDescending(match => match.ExactMatch)
            .ThenBy(match => match.PrefixRank)
            .ThenBy(match => match.Suggestion.Label, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(match => match.Suggestion.ClassId, StringComparer.Ordinal)
            .Take(limit)
            .Select(match => match.Suggestion)
            .ToArray();
    }

    public async Task<ProjectResourceTypeSearchPage> SearchAsync(
        string classId,
        ProjectAccessSnapshot access,
        int offset = 0,
        int limit = 50,
        string preferredLanguage = "ru",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(classId);
        ArgumentNullException.ThrowIfNull(access);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, 100);
        ArgumentException.ThrowIfNullOrWhiteSpace(preferredLanguage);

        if (!ontology.TryGetTerm(classId, out OntologyTerm? selected) ||
            selected?.Kind != OntologyTermKind.Class)
        {
            throw new ArgumentException(
                $"Ontology class was not found: {classId}",
                nameof(classId));
        }

        IReadOnlySet<string> readable = ProjectAuthorization.RequireSearch(access);
        HashSet<string> cassetteIds = ProjectSearchRules.EffectiveCassetteIds(readable);
        HashSet<string> resourceIds = await FindResourceIdsAsync(
            classId,
            cassetteIds,
            cancellationToken);
        List<ProjectResourceSummary> summaries = [];
        foreach (string resourceId in resourceIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProjectResourceSummary? summary = await _summaries.ReadAsync(
                resourceId,
                cassetteIds,
                preferredLanguage,
                cancellationToken);
            if (summary is not null)
            {
                summaries.Add(summary);
            }
        }

        ProjectResourceSummary[] ordered = summaries
            .OrderBy(summary => summary.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(summary => summary.ResourceId, StringComparer.Ordinal)
            .ToArray();
        ProjectResourceSearchResult[] page = ordered
            .Skip(offset)
            .Take(limit)
            .Select(summary => new ProjectResourceSearchResult(
                summary.ResourceId,
                summary.DisplayName,
                summary.Type,
                summary.TypeLabel,
                0,
                summary.SourceCassetteId,
                Array.Empty<ProjectSearchEvidence>()))
            .ToArray();

        return new ProjectResourceTypeSearchPage(
            classId,
            ontology.LabelOf(classId, preferredLanguage) ?? classId,
            ordered.Length,
            offset,
            limit,
            page);
    }

    private async Task<HashSet<string>> FindResourceIdsAsync(
        string classId,
        IReadOnlySet<string> cassetteIds,
        CancellationToken cancellationToken)
    {
        string[] assignableTypes = ontology.Terms
            .Where(term => term.Kind == OntologyTermKind.Class)
            .Where(term => ontology.AncestorsAndSelf(term.Id)
                .Contains(classId, StringComparer.Ordinal))
            .Select(term => term.Id)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        HashSet<string> resourceIds = new(StringComparer.Ordinal);

        foreach (string typeId in assignableTypes)
        {
            await foreach (TripleRow triple in rdfStore.FindAsync(
                               new TriplePattern(
                                   Predicate: RdfType,
                                   ObjectKind: TripleObjectKind.Iri,
                                   ObjectValue: typeId),
                               cassetteIds,
                               cancellationToken))
            {
                resourceIds.Add(triple.Subject);
            }
        }

        return resourceIds;
    }

    private ClassMatch? Match(
        OntologyTerm term,
        string normalizedQuery,
        string preferredLanguage)
    {
        string label = ontology.LabelOf(term.Id, preferredLanguage) ?? term.Id;
        string[] values = term.Labels
            .Select(value => value.Value)
            .Append(label)
            .Append(TerminalId(term.Id))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        bool exact = values.Any(value => Normalize(value) == normalizedQuery);
        int prefixRank = values
            .Select(value => Normalize(value))
            .Where(value => value.StartsWith(normalizedQuery, StringComparison.Ordinal))
            .Select(value => value.Length - normalizedQuery.Length)
            .DefaultIfEmpty(int.MaxValue)
            .Min();
        if (!exact && prefixRank == int.MaxValue)
        {
            return null;
        }

        return new ClassMatch(
            new OntologyClassSearchSuggestion(
                term.Id,
                label,
                exact,
                term.IsAbstract),
            exact ? 0 : prefixRank);
    }

    private static string Normalize(string value)
    {
        string normalized = value
            .Normalize(NormalizationForm.FormKC)
            .Trim()
            .ToLowerInvariant()
            .Replace('ё', 'е');
        StringBuilder result = new(normalized.Length);
        bool previousWhitespace = false;
        foreach (char symbol in normalized)
        {
            if (char.IsWhiteSpace(symbol))
            {
                if (!previousWhitespace && result.Length > 0)
                {
                    result.Append(' ');
                }
                previousWhitespace = true;
                continue;
            }

            previousWhitespace = false;
            result.Append(symbol);
        }

        return result.ToString().TrimEnd();
    }

    private static string TerminalId(string id)
    {
        int slash = id.LastIndexOf('/');
        int hash = id.LastIndexOf('#');
        int separator = Math.Max(slash, hash);
        return separator < 0 || separator == id.Length - 1
            ? id
            : id[(separator + 1)..];
    }

    private sealed record ClassMatch(
        OntologyClassSearchSuggestion Suggestion,
        int PrefixRank)
    {
        public bool ExactMatch => Suggestion.ExactMatch;
    }
}
