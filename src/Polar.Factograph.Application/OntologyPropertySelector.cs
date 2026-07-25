namespace Polar.Factograph.Application;

internal sealed class OntologyPropertySelector(
    IReadOnlyDictionary<string, OntologyTerm> terms,
    OntologyClassHierarchy hierarchy)
{
    public IReadOnlyList<OntologyTerm> DirectForType(string classId)
    {
        HashSet<string> ancestors = new(hierarchy.AncestorsAndSelf(classId), StringComparer.Ordinal);
        return terms.Values
            .Where(term =>
                term.Kind is OntologyTermKind.DatatypeProperty or OntologyTermKind.ObjectProperty &&
                term.Domains.Any(ancestors.Contains))
            .OrderBy(term => term.Priority is null)
            .ThenBy(term => term.Priority, StringComparer.Ordinal)
            .ThenBy(term => term.Id, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<OntologyTerm> InverseForType(string classId)
    {
        HashSet<string> ancestors = new(hierarchy.AncestorsAndSelf(classId), StringComparer.Ordinal);
        return terms.Values
            .Where(term =>
                term.Kind == OntologyTermKind.ObjectProperty &&
                term.Ranges.Any(ancestors.Contains))
            .OrderBy(term => term.Priority is null)
            .ThenBy(term => term.Priority, StringComparer.Ordinal)
            .ThenBy(term => term.Id, StringComparer.Ordinal)
            .ToArray();
    }
}
