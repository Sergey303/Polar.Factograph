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
    private const string EntityTypeRoot = "http://fogid.net/o/sys-obj";

    public OntologyValidationReport Validate(OntologyCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return Validate(catalog.Terms);
    }

    public OntologyValidationReport Validate(IEnumerable<OntologyTerm> sourceTerms)
    {
        ArgumentNullException.ThrowIfNull(sourceTerms);
        Dictionary<string, OntologyTerm> terms = sourceTerms
            .ToDictionary(term => term.Id, StringComparer.Ordinal);
        List<OntologyValidationIssue> issues = [];

        ValidateEntityRoot(terms, issues);
        foreach (OntologyTerm term in terms.Values.OrderBy(term => term.Id, StringComparer.Ordinal))
        {
            ValidateLabel(term, issues);
            switch (term.Kind)
            {
                case OntologyTermKind.Class:
                    ValidateClassReference(term, terms, issues);
                    break;
                case OntologyTermKind.DatatypeProperty:
                    ValidateProperty(term, terms, resourceProperty: false, issues);
                    break;
                case OntologyTermKind.ObjectProperty:
                    ValidateProperty(term, terms, resourceProperty: true, issues);
                    break;
                case OntologyTermKind.EnumerationType:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(term.Kind), term.Kind, null);
            }
        }

        ValidateClassCycles(terms, issues);

        OntologyValidationIssue[] ordered = issues
            .OrderBy(issue => issue.Severity == OntologyValidationSeverities.Error ? 0 : 1)
            .ThenBy(issue => issue.TermId, StringComparer.Ordinal)
            .ThenBy(issue => issue.Code, StringComparer.Ordinal)
            .ToArray();
        return new OntologyValidationReport(
            terms.Count,
            ordered.Count(issue => issue.Severity == OntologyValidationSeverities.Error),
            ordered.Count(issue => issue.Severity == OntologyValidationSeverities.Warning),
            ordered);
    }

    private static void ValidateEntityRoot(
        IReadOnlyDictionary<string, OntologyTerm> terms,
        ICollection<OntologyValidationIssue> issues)
    {
        if (terms.TryGetValue(EntityTypeRoot, out OntologyTerm? root) &&
            root.Kind == OntologyTermKind.Class)
        {
            return;
        }

        AddError(
            issues,
            "missing_entity_root",
            EntityTypeRoot,
            "Корневой класс системных сущностей отсутствует или не является классом; универсальный интерфейс не сможет определить доступные типы сущностей.");
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

    private static void ValidateClassReference(
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

    private static void ValidateClassCycles(
        IReadOnlyDictionary<string, OntologyTerm> terms,
        ICollection<OntologyValidationIssue> issues)
    {
        Dictionary<string, int> states = new(StringComparer.Ordinal);
        Dictionary<string, int> positions = new(StringComparer.Ordinal);
        List<string> stack = [];

        foreach (OntologyTerm term in terms.Values.Where(term => term.Kind == OntologyTermKind.Class))
        {
            if (!states.ContainsKey(term.Id))
            {
                Visit(term.Id);
            }
        }

        void Visit(string classId)
        {
            states[classId] = 1;
            positions[classId] = stack.Count;
            stack.Add(classId);

            OntologyTerm current = terms[classId];
            string? parentId = current.ParentClassId;
            if (parentId is not null &&
                terms.TryGetValue(parentId, out OntologyTerm? parent) &&
                parent.Kind == OntologyTermKind.Class)
            {
                if (!states.TryGetValue(parentId, out int state))
                {
                    Visit(parentId);
                }
                else if (state == 1 && positions.TryGetValue(parentId, out int start))
                {
                    string[] cycle = stack.Skip(start).Append(parentId).ToArray();
                    AddError(
                        issues,
                        "cyclic_class_hierarchy",
                        classId,
                        $"Обнаружен цикл наследования классов: {string.Join(" -> ", cycle)}.");
                }
            }

            stack.RemoveAt(stack.Count - 1);
            positions.Remove(classId);
            states[classId] = 2;
        }
    }

    private static void ValidateProperty(
        OntologyTerm term,
        IReadOnlyDictionary<string, OntologyTerm> terms,
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

        if (hasValidRange && !HasConcreteEntityTarget(term, terms))
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

    private static bool HasConcreteEntityTarget(
        OntologyTerm property,
        IReadOnlyDictionary<string, OntologyTerm> terms) => terms.Values.Any(candidate =>
            candidate.Kind == OntologyTermKind.Class &&
            !candidate.IsAbstract &&
            IsDescendantOrSelf(candidate.Id, EntityTypeRoot, terms) &&
            property.Ranges.Any(range => IsDescendantOrSelf(candidate.Id, range, terms)));

    private static bool IsDescendantOrSelf(
        string classId,
        string ancestorId,
        IReadOnlyDictionary<string, OntologyTerm> terms)
    {
        HashSet<string> visited = new(StringComparer.Ordinal);
        string? currentId = classId;
        while (currentId is not null && visited.Add(currentId))
        {
            if (string.Equals(currentId, ancestorId, StringComparison.Ordinal))
            {
                return true;
            }

            if (!terms.TryGetValue(currentId, out OntologyTerm? current) ||
                current.Kind != OntologyTermKind.Class)
            {
                return false;
            }
            currentId = current.ParentClassId;
        }

        return false;
    }

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
