namespace Polar.Factograph.Application;

internal sealed class OntologyEnumerationLabelResolver(
    IReadOnlyDictionary<string, OntologyTerm> terms)
{
    public string? Resolve(
        string propertyId,
        string value,
        string preferredLanguage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (!terms.TryGetValue(propertyId, out OntologyTerm? property))
        {
            return null;
        }

        OntologyTerm? enumeration = property.Ranges
            .Select(range => terms.TryGetValue(range, out OntologyTerm? term) ? term : null)
            .FirstOrDefault(term => term?.Kind == OntologyTermKind.EnumerationType);
        if (enumeration is null)
        {
            return null;
        }

        OntologyLocalizedText[] labels = enumeration.EnumerationStates
            .Where(state => string.Equals(state.Value, value, StringComparison.Ordinal))
            .Select(state => new OntologyLocalizedText(state.Label, state.Language))
            .ToArray();
        return OntologyLocalization.Select(labels, preferredLanguage);
    }
}
