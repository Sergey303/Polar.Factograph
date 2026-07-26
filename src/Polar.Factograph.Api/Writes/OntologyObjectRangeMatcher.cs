using Polar.Factograph.Application;

namespace Polar.Factograph.Api.Writes;

internal static class OntologyObjectRangeMatcher
{
    public static bool Matches(
        OntologyCatalog catalog,
        OntologyTerm property,
        IReadOnlyList<string> targetTypeIds)
    {
        if (property.Ranges.Count == 0)
        {
            return true;
        }

        OntologyTerm[] ranges = property.Ranges
            .Select(id => ResolveClass(catalog, id))
            .Where(term => term is not null)
            .Cast<OntologyTerm>()
            .ToArray();
        if (ranges.Length == 0)
        {
            throw new InvalidDataException(
                $"Object property '{property.Id}' has no class range in the ontology.");
        }

        foreach (string targetTypeId in targetTypeIds)
        {
            OntologyTerm? targetType = ResolveClass(catalog, targetTypeId);
            if (targetType is null)
            {
                continue;
            }

            IReadOnlyList<string> ancestors = catalog.AncestorsAndSelf(targetType.Id);
            if (ranges.Any(range => ancestors.Contains(range.Id, StringComparer.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }

    private static OntologyTerm? ResolveClass(OntologyCatalog catalog, string id) =>
        OntologyWriteTermResolver.TryResolve(catalog, id, out OntologyTerm? term) &&
        term?.Kind == OntologyTermKind.Class
            ? term
            : null;
}
