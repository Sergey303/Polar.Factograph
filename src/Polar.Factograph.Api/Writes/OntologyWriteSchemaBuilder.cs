using Polar.Factograph.Application;

namespace Polar.Factograph.Api.Writes;

public sealed class OntologyWriteSchemaBuilder
{
    public OntologyWriteSchemaResponse Build(
        OntologyCatalog catalog,
        string preferredLanguage)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentException.ThrowIfNullOrWhiteSpace(preferredLanguage);

        OntologyWriteClassResponse[] classes = catalog.Terms
            .Where(term => term.Kind == OntologyTermKind.Class)
            .Select(term => BuildClass(catalog, term, preferredLanguage))
            .OrderBy(item => item.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();
        return new OntologyWriteSchemaResponse(classes);
    }

    private static OntologyWriteClassResponse BuildClass(
        OntologyCatalog catalog,
        OntologyTerm type,
        string language)
    {
        OntologyWritePropertyResponse[] properties = catalog
            .DirectPropertiesForType(type.Id)
            .Where(IsWritableProperty)
            .Select(property => BuildProperty(catalog, property, language))
            .ToArray();
        return new OntologyWriteClassResponse(
            type.Id,
            catalog.LabelOf(type.Id, language) ?? type.Id,
            type.ParentClassId,
            properties);
    }

    private static OntologyWritePropertyResponse BuildProperty(
        OntologyCatalog catalog,
        OntologyTerm property,
        string language) => new(
        property.Id,
        catalog.LabelOf(property.Id, language) ?? property.Id,
        property.Kind == OntologyTermKind.ObjectProperty ? "resource" : "literal",
        property.Ranges.ToArray(),
        BuildOptions(catalog, property, language));

    private static OntologyWriteOptionResponse[] BuildOptions(
        OntologyCatalog catalog,
        OntologyTerm property,
        string language) => property.Ranges
        .Select(range => catalog.TryGetTerm(range, out OntologyTerm? term) ? term : null)
        .Where(term => term?.Kind == OntologyTermKind.EnumerationType)
        .SelectMany(term => term!.EnumerationStates)
        .GroupBy(state => state.Value, StringComparer.Ordinal)
        .Select(group => new OntologyWriteOptionResponse(
            group.Key,
            catalog.EnumerationLabel(property.Id, group.Key, language)
                ?? SelectFallbackLabel(group, language)
                ?? group.Key))
        .OrderBy(option => option.Label, StringComparer.OrdinalIgnoreCase)
        .ThenBy(option => option.Value, StringComparer.Ordinal)
        .ToArray();

    private static string? SelectFallbackLabel(
        IEnumerable<OntologyEnumerationState> states,
        string language)
    {
        OntologyEnumerationState[] values = states.ToArray();
        return values.FirstOrDefault(state =>
                   string.Equals(state.Language, language, StringComparison.OrdinalIgnoreCase))?.Label
               ?? values.FirstOrDefault(state => state.Language is null)?.Label
               ?? values.FirstOrDefault()?.Label;
    }

    private static bool IsWritableProperty(OntologyTerm term) =>
        term.Kind is OntologyTermKind.DatatypeProperty or OntologyTermKind.ObjectProperty;
}