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
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (catalog.TryGetTerm(id, out OntologyTerm? exact) && exact is not null)
        {
            return exact;
        }

        string alternate = id.StartsWith(
            LegacyFogVocabulary.Namespace,
            StringComparison.Ordinal)
            ? id[LegacyFogVocabulary.Namespace.Length..]
            : LegacyFogVocabulary.Namespace + id;
        if (catalog.TryGetTerm(alternate, out OntologyTerm? resolved) && resolved is not null)
        {
            return resolved;
        }

        throw new ArgumentException(
            $"Ontology does not define {description} '{id}'.");
    }
}
