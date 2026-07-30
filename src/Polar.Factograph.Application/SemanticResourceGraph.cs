namespace Polar.Factograph.Application;

internal sealed class SemanticResourceGraph(
    AuthorizedProjectReadService reads,
    OntologyResourcePortraitPresenter presenter,
    OntologyCatalog ontology,
    ProjectAccessSnapshot access,
    string preferredLanguage)
{
    private readonly Dictionary<string, ProjectResourcePortrait?> _cache =
        new(StringComparer.Ordinal);

    public async ValueTask<ProjectResourcePortrait?> GetAsync(
        string resourceId,
        CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(resourceId, out ProjectResourcePortrait? cached))
        {
            return cached;
        }

        ProjectResourcePortrait? portrait = await reads.GetPortraitAsync(
            resourceId,
            access,
            cancellationToken);
        _cache[resourceId] = portrait;
        return portrait;
    }

    public PresentedProjectResourcePortrait Present(ProjectResourcePortrait portrait) =>
        presenter.Present(portrait, preferredLanguage);

    public bool IsType(ProjectResourcePortrait portrait, string classId)
    {
        if (portrait.Type is null ||
            !ontology.TryGetTerm(portrait.Type, out OntologyTerm? term) ||
            term?.Kind != OntologyTermKind.Class)
        {
            return false;
        }

        return ontology.AncestorsAndSelf(portrait.Type)
            .Contains(classId, StringComparer.Ordinal);
    }

    public bool IsEntity(ProjectResourcePortrait portrait) =>
        IsType(portrait, SemanticBridgeVocabulary.SystemObject);

    public bool IsComplexRelation(ProjectResourcePortrait portrait) =>
        IsType(portrait, SemanticBridgeVocabulary.Entity) &&
        !IsEntity(portrait);

    public bool IsTechnical(ProjectResourcePortrait portrait) =>
        portrait.Type is not null &&
        SemanticBridgeVocabulary.TechnicalTypes.Contains(portrait.Type);

    public string DisplayName(ProjectResourcePortrait portrait)
    {
        ResourceLiteralField? preferred = portrait.Literals.FirstOrDefault(field =>
            string.Equals(field.Predicate, SemanticBridgeVocabulary.Name, StringComparison.Ordinal) &&
            string.Equals(field.Language, preferredLanguage, StringComparison.OrdinalIgnoreCase));
        ResourceLiteralField? anyName = portrait.Literals.FirstOrDefault(field =>
            string.Equals(field.Predicate, SemanticBridgeVocabulary.Name, StringComparison.Ordinal));
        return preferred?.Value ?? anyName?.Value ?? portrait.ResourceId;
    }

    public string? TypeLabel(ProjectResourcePortrait portrait) =>
        portrait.Type is null
            ? null
            : ontology.LabelOf(portrait.Type, preferredLanguage) ?? portrait.Type;

    public string PropertyLabel(string predicate) =>
        ontology.LabelOf(predicate, preferredLanguage) ?? predicate;

    public string InversePropertyLabel(string predicate) =>
        ontology.InverseLabelOf(predicate, preferredLanguage) ?? PropertyLabel(predicate);

    public string? DocumentUri(ProjectResourcePortrait portrait) =>
        portrait.Literals
            .Where(field => string.Equals(
                field.Predicate,
                SemanticBridgeVocabulary.Uri,
                StringComparison.Ordinal))
            .Select(field => field.Value.Trim())
            .FirstOrDefault(value => value.StartsWith("iiss://", StringComparison.OrdinalIgnoreCase))
        ?? portrait.Literals
            .Select(field => field.Value.Trim())
            .FirstOrDefault(value => value.StartsWith("iiss://", StringComparison.OrdinalIgnoreCase));

    public SemanticDateValue? DateValue(ProjectResourcePortrait portrait)
    {
        SemanticDateValue? from = SemanticDateParser.Parse(
            LiteralValue(portrait, SemanticBridgeVocabulary.FromDate));
        SemanticDateValue? to = SemanticDateParser.Parse(
            LiteralValue(portrait, SemanticBridgeVocabulary.ToDate));
        if (from is not null)
        {
            return to is null
                ? from
                : new SemanticDateValue($"{from.Display}–{to.Display}", from.SortKey);
        }

        SemanticDateValue? earliest = null;
        foreach (ResourceLiteralField field in portrait.Literals)
        {
            if (!IsDateProperty(field.Predicate))
            {
                continue;
            }

            SemanticDateValue? value = SemanticDateParser.Parse(field.Value);
            if (value is not null &&
                (earliest is null || string.CompareOrdinal(value.SortKey, earliest.SortKey) < 0))
            {
                earliest = value;
            }
        }

        return earliest;
    }

    public IReadOnlyList<string> DirectTargets(
        ProjectResourcePortrait portrait,
        string predicate) =>
        portrait.DirectLinks
            .Where(link => string.Equals(link.Predicate, predicate, StringComparison.Ordinal))
            .Select(link => link.TargetResourceId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    public IReadOnlyList<string> InverseSources(
        ProjectResourcePortrait portrait,
        string predicate) =>
        portrait.InverseLinks
            .Where(link => string.Equals(link.Predicate, predicate, StringComparison.Ordinal))
            .Select(link => link.SourceResourceId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    public string? LiteralValue(ProjectResourcePortrait portrait, string predicate) =>
        portrait.Literals
            .Where(field => string.Equals(field.Predicate, predicate, StringComparison.Ordinal))
            .Select(field => field.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    public async ValueTask<ProjectResourcePortrait> ResolveCanonicalAsync(
        ProjectResourcePortrait portrait,
        CancellationToken cancellationToken)
    {
        string? targetId = portrait.Type switch
        {
            SemanticBridgeVocabulary.Reflection => DirectTargets(
                portrait,
                SemanticBridgeVocabulary.Reflected).FirstOrDefault(),
            SemanticBridgeVocabulary.Participation => DirectTargets(
                portrait,
                SemanticBridgeVocabulary.Participant).FirstOrDefault(),
            SemanticBridgeVocabulary.CollectionMember => DirectTargets(
                portrait,
                SemanticBridgeVocabulary.CollectionItem).FirstOrDefault(),
            _ => null
        };

        if (targetId is null)
        {
            return portrait;
        }

        ProjectResourcePortrait? target = await GetAsync(targetId, cancellationToken);
        return target is null || IsTechnical(target) ? portrait : target;
    }

    public async ValueTask<SemanticResourceLink?> LinkAsync(
        string resourceId,
        string relationLabel,
        CancellationToken cancellationToken,
        ProjectResourcePortrait? relation = null,
        string? groupKey = null,
        string? groupLabel = null)
    {
        ProjectResourcePortrait? portrait = await GetAsync(resourceId, cancellationToken);
        if (portrait is null || IsTechnical(portrait))
        {
            return null;
        }

        string? documentUri = DocumentUri(portrait);
        SemanticDateValue? date = relation is null ? null : DateValue(relation);
        if (date is null && documentUri is not null)
        {
            date = DateValue(portrait);
        }

        string effectiveGroupKey = groupKey
            ?? relation?.Type
            ?? relationLabel;
        string effectiveGroupLabel = groupLabel
            ?? (relation is null ? relationLabel : TypeLabel(relation))
            ?? relationLabel;

        return new SemanticResourceLink(
            portrait.ResourceId,
            DisplayName(portrait),
            portrait.Type,
            TypeLabel(portrait),
            relationLabel,
            relation?.ResourceId,
            documentUri,
            date?.Display,
            date?.SortKey,
            effectiveGroupKey,
            effectiveGroupLabel);
    }

    private bool IsDateProperty(string predicate)
    {
        if (string.Equals(predicate, SemanticBridgeVocabulary.FromDate, StringComparison.Ordinal) ||
            string.Equals(predicate, SemanticBridgeVocabulary.ToDate, StringComparison.Ordinal) ||
            string.Equals(predicate, SemanticBridgeVocabulary.Date, StringComparison.Ordinal))
        {
            return true;
        }

        return ontology.TryGetTerm(predicate, out OntologyTerm? term) &&
            term is { Kind: OntologyTermKind.DatatypeProperty } &&
            term.Ranges.Contains(SemanticBridgeVocabulary.DateDataType, StringComparer.Ordinal);
    }
}
