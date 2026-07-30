namespace Polar.Factograph.Application;

public static class OntologyValidationSeverities
{
    public const string Error = "error";
    public const string Warning = "warning";
}

public sealed record OntologyValidationIssue(
    string Severity,
    string Code,
    string TermId,
    string Message);

public sealed record OntologyValidationReport(
    int TermCount,
    int ErrorCount,
    int WarningCount,
    IReadOnlyList<OntologyValidationIssue> Issues)
{
    public bool IsValid => ErrorCount == 0;
}

public sealed class OntologyValidationService
{
    public OntologyValidationReport Validate(OntologyCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        List<OntologyValidationIssue> issues = [];
        Dictionary<string, OntologyTerm> terms = catalog.Terms
            .ToDictionary(term => term.Id, StringComparer.Ordinal);

        foreach (OntologyTerm term in catalog.Terms.OrderBy(term => term.Id, StringComparer.Ordinal))
        {
            ValidateLabel(term, issues);
            switch (term.Kind)
            {
                case OntologyTermKind.Class:
                    ValidateClass(term, terms, issues);
                    break;
                case OntologyTermKind.DatatypeProperty:
                    ValidateProperty(term, terms, catalog, resourceProperty: false, issues);
                    break;
                case OntologyTermKind.ObjectProperty:
                    ValidateProperty(term, terms, catalog, resourceProperty: true, issues);
                    break;
                case OntologyTermKind.EnumerationValue:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(term.Kind), term.Kind, null);
            }
        }

        OntologyValidationIssue[] ordered = issues
            .OrderBy(issue => issue.Severity == OntologyValidationSeverities.Error ? 0 : 1)
            .ThenBy(issue => issue.TermId, StringComparer.Ordinal)
            .ThenBy(issue => issue.Code, StringComparer.Ordinal)
            .ToArray();
        return new OntologyValidationReport(
            catalog.Terms.Count,
            ordered.Count(issue => issue.Severity == OntologyValidationSeverities.Error),
            ordered.Count(issue => issue.Severity == OntologyValidationSeverities.Warning),
            ordered);
    }

    private static void ValidateLabel(
        OntologyTerm term,
        ICollection<OntologyValidationIssue> issues)
    {
        if (term.Labels.Any(label => !string.IsNullOrWhiteSpace(label.Value)))
        {
            return;
        }

        AddWarning(
            issues,
            "missing_label",
            term.Id,
            "Термин не имеет подписи; публичный интерфейс будет показывать его URI.");
    }

    private static void ValidateClass(
        OntologyTerm term,
        IReadOnlyDictionary<string, OntologyTerm> terms,
        ICollection<OntologyValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(term.ParentClassId))
        {
            return;
        }

        if (!terms.TryGetValue(term.ParentClassId, out OntologyTerm? parent))
        {
            AddError(
                issues,
                "missing_parent_class",
                term.Id,
                $"Родительский класс '{term.ParentClassId}' отсутствует в онтологии.");
            return;
        }

        if (parent.Kind != OntologyTermKind.Class)
        {
            AddError(
                issues,
                "parent_is_not_class",
                term.Id,
                $"Родитель '{term.ParentClassId}' существует, но не является классом.");
        }
    }

    private static void ValidateProperty(
        OntologyTerm term,
        IReadOnlyDictionary<string, OntologyTerm> terms,
        OntologyCatalog catalog,
        bool resourceProperty,
        ICollection<OntologyValidationIssue> issues)
    {
        if (term.Domains.Count == 0)
        {
            AddWarning(
                issues,
                "missing_domain",
                term.Id,
                "Свойство не имеет domain и не появится в универсальной форме сущности.");
        }

        foreach (string domainId in term.Domains.Distinct(StringComparer.Ordinal))
        {
            if (!terms.TryGetValue(domainId, out OntologyTerm? domain))
            {
                AddError(
                    issues,
                    "missing_domain_class",
                    term.Id,
                    $"Domain '{domainId}' отсутствует в онтологии.");
            }
            else if (domain.Kind != OntologyTermKind.Class)
            {
                AddError(
                    issues,
                    "domain_is_not_class",
                    term.Id,
                    $"Domain '{domainId}' существует, но не является классом.");
            }
        }

        if (term.Ranges.Count == 0)
        {
            AddWarning(
                issues,
                "missing_range",
                term.Id,
                resourceProperty
                    ? "Ссылочное свойство не имеет range; пикер не сможет предложить допустимые типы сущностей."
                    : "Литеральное свойство не имеет range и будет редактироваться как обычный текст.");
            return;
        }

        if (!resourceProperty)
        {
            return;
        }

        bool hasValidRange = false;
        foreach (string rangeId in term.Ranges.Distinct(StringComparer.Ordinal))
        {
            if (!terms.TryGetValue(rangeId, out OntologyTerm? range))
            {
                AddError(
                    issues,
                    "missing_range_class",
                    term.Id,
                    $"Range '{rangeId}' отсутствует в онтологии.");
            }
            else if (range.Kind != OntologyTermKind.Class)
            {
                AddError(
                    issues,
                    "range_is_not_class",
                    term.Id,
                    $"Range '{rangeId}' существует, но не является классом.");
            }
            else
            {
                hasValidRange = true;
            }
        }

        if (hasValidRange && !HasConcreteEntityTarget(term, catalog))
        {
            AddError(
                issues,
                "no_concrete_entity_target",
                term.Id,
                "Ни один конкретный класс сущности не соответствует range; ссылочное поле нельзя заполнить через универсальный пикер.");
        }

        if (!term.InverseLabels.Any(label => !string.IsNullOrWhiteSpace(label.Value)))
        {
            AddWarning(
                issues,
                "missing_inverse_label",
                term.Id,
                "Свойство не имеет inverse-label; входящая связь будет подписана общей подписью свойства.");
        }
    }

    private static bool HasConcreteEntityTarget(OntologyTerm property, OntologyCatalog catalog) =>
        catalog.Terms.Any(candidate =>
            candidate.Kind == OntologyTermKind.Class &&
            candidate.IsEntityType &&
            !candidate.IsAbstract &&
            property.Ranges.Any(range => catalog.AncestorsAndSelf(candidate.Id)
                .Contains(range, StringComparer.Ordinal)));

    private static void AddError(
        ICollection<OntologyValidationIssue> issues,
        string code,
        string termId,
        string message) => issues.Add(new OntologyValidationIssue(
            OntologyValidationSeverities.Error,
            code,
            termId,
            message));

    private static void AddWarning(
        ICollection<OntologyValidationIssue> issues,
        string code,
        string termId,
        string message) => issues.Add(new OntologyValidationIssue(
            OntologyValidationSeverities.Warning,
            code,
            termId,
            message));
}
