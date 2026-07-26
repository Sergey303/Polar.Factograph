using Polar.Factograph.Application;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Writes;

internal static class OntologyWriteTermResolver
{
    public static OntologyTerm Require(
        OntologyCatalog catalog,
        string id,
        string description)
    {
        if (TryResolve(catalog, id, out OntologyTerm? term) && term is not null)
        {
            return term;
        }

        throw new ArgumentException(
            $"Ontology does not define {description} '{id}'.");
    }

    public static bool TryResolve(
        OntologyCatalog catalog,
        string id,
        out OntologyTerm? term)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (catalog.TryGetTerm(id, out term) && term is not null)
        {
            return true;
        }

        string alternate = id.StartsWith(
            LegacyFogVocabulary.Namespace,
            StringComparison.Ordinal)
            ? id[LegacyFogVocabulary.Namespace.Length..]
            : LegacyFogVocabulary.Namespace + id;
        return catalog.TryGetTerm(alternate, out term) && term is not null;
    }
}
