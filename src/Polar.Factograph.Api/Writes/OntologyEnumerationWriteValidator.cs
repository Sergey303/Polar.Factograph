using Polar.Factograph.Application;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Writes;

internal static class OntologyEnumerationWriteValidator
{
    public static void Validate(
        OntologyCatalog catalog,
        OntologyTerm propertyTerm,
        FogProperty property)
    {
        foreach (string rangeId in propertyTerm.Ranges)
        {
            if (!catalog.TryGetTerm(rangeId, out OntologyTerm? range) ||
                range is null ||
                range.Kind != OntologyTermKind.EnumerationType)
            {
                continue;
            }

            if (!range.EnumerationStates.Any(state =>
                    string.Equals(state.Value, property.Value, StringComparison.Ordinal)))
            {
                throw new ArgumentException(
                    $"Value '{property.Value}' is not allowed for ontology property '{propertyTerm.Id}'.");
            }
        }
    }
}
