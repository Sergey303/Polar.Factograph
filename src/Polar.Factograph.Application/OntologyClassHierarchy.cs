namespace Polar.Factograph.Application;

internal sealed class OntologyClassHierarchy(
    IReadOnlyDictionary<string, OntologyTerm> terms)
{
    public IReadOnlyList<string> AncestorsAndSelf(string classId)
    {
        OntologyTerm current = RequireClass(classId);
        List<string> result = new() { current.Id };
        HashSet<string> visited = new(StringComparer.Ordinal) { current.Id };

        while (current.ParentClassId is not null)
        {
            current = RequireClass(current.ParentClassId);
            if (!visited.Add(current.Id))
            {
                throw new InvalidDataException(
                    $"Cyclic ontology class hierarchy: {string.Join(" -> ", result.Append(current.Id))}");
            }

            result.Add(current.Id);
        }

        result.Reverse();
        return result;
    }

    public void Validate()
    {
        foreach (OntologyTerm term in terms.Values.Where(
                     term => term.Kind == OntologyTermKind.Class))
        {
            _ = AncestorsAndSelf(term.Id);
        }
    }

    private OntologyTerm RequireClass(string id)
    {
        if (!terms.TryGetValue(id, out OntologyTerm? term) ||
            term.Kind != OntologyTermKind.Class)
        {
            throw new KeyNotFoundException($"Ontology class was not found: {id}");
        }

        return term;
    }
}
