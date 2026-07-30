namespace Polar.Factograph.Application;

public sealed record PresentedResourceLiteralField(
    string Predicate,
    string Label,
    string Value,
    string DisplayValue,
    string? Language,
    string? DataType);

public sealed record PresentedResourceDirectLink(
    string Predicate,
    string Label,
    string TargetResourceId);

public sealed record PresentedResourceInverseLink(
    string Predicate,
    string Label,
    string SourceResourceId,
    string SourceCassetteId);

public sealed record PresentedProjectResourcePortrait(
    string ResourceId,
    string? Type,
    string? TypeLabel,
    IReadOnlyList<PresentedResourceLiteralField> Literals,
    IReadOnlyList<PresentedResourceDirectLink> DirectLinks,
    IReadOnlyList<PresentedResourceInverseLink> InverseLinks,
    PresentedResourceProvenance? Provenance);

/// <summary>
/// Converts a raw RDF portrait into a stable, ontology-labelled view model without changing stored values.
/// </summary>
public sealed class OntologyResourcePortraitPresenter
{
    private readonly OntologyCatalog _ontology;

    public OntologyResourcePortraitPresenter(OntologyCatalog ontology)
    {
        _ontology = ontology ?? throw new ArgumentNullException(nameof(ontology));
    }

    public PresentedProjectResourcePortrait Present(
        ProjectResourcePortrait portrait,
        string preferredLanguage = "ru",
        ResourceProvenanceDetail provenanceDetail = ResourceProvenanceDetail.None)
    {
        ArgumentNullException.ThrowIfNull(portrait);
        ArgumentException.ThrowIfNullOrWhiteSpace(preferredLanguage);

        IReadOnlyDictionary<string, int> directOrder = BuildPropertyOrder(
            portrait.Type,
            inverse: false);
        IReadOnlyDictionary<string, int> inverseOrder = BuildPropertyOrder(
            portrait.Type,
            inverse: true);

        PresentedResourceLiteralField[] literals = portrait.Literals
            .OrderBy(field => PropertyPosition(directOrder, field.Predicate))
            .ThenBy(field => field.Predicate, StringComparer.Ordinal)
            .ThenBy(field => field.Language ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(field => field.Value, StringComparer.Ordinal)
            .Select(field => new PresentedResourceLiteralField(
                field.Predicate,
                PropertyLabel(field.Predicate, preferredLanguage),
                field.Value,
                DisplayLiteralValue(field, preferredLanguage),
                field.Language,
                field.DataType))
            .ToArray();

        PresentedResourceDirectLink[] directLinks = portrait.DirectLinks
            .OrderBy(link => PropertyPosition(directOrder, link.Predicate))
            .ThenBy(link => link.Predicate, StringComparer.Ordinal)
            .ThenBy(link => link.TargetResourceId, StringComparer.Ordinal)
            .Select(link => new PresentedResourceDirectLink(
                link.Predicate,
                PropertyLabel(link.Predicate, preferredLanguage),
                link.TargetResourceId))
            .ToArray();

        PresentedResourceInverseLink[] inverseLinks = portrait.InverseLinks
            .OrderBy(link => PropertyPosition(inverseOrder, link.Predicate))
            .ThenBy(link => link.Predicate, StringComparer.Ordinal)
            .ThenBy(link => link.SourceResourceId, StringComparer.Ordinal)
            .ThenBy(link => link.SourceCassetteId, StringComparer.Ordinal)
            .Select(link => new PresentedResourceInverseLink(
                link.Predicate,
                _ontology.InverseLabelOf(link.Predicate, preferredLanguage)
                    ?? PropertyLabel(link.Predicate, preferredLanguage),
                link.SourceResourceId,
                link.SourceCassetteId))
            .ToArray();

        return new PresentedProjectResourcePortrait(
            portrait.ResourceId,
            portrait.Type,
            portrait.Type is null
                ? null
                : _ontology.LabelOf(portrait.Type, preferredLanguage) ?? portrait.Type,
            literals,
            directLinks,
            inverseLinks,
            ResourceProvenancePresentation.Present(portrait.Provenance, provenanceDetail));
    }

    private IReadOnlyDictionary<string, int> BuildPropertyOrder(
        string? type,
        bool inverse)
    {
        if (type is null ||
            !_ontology.TryGetTerm(type, out OntologyTerm? term) ||
            term?.Kind != OntologyTermKind.Class)
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }

        IReadOnlyList<OntologyTerm> properties = inverse
            ? _ontology.InversePropertiesForType(type)
            : _ontology.DirectPropertiesForType(type);

        return properties
            .Select((property, index) => new { property.Id, Index = index })
            .ToDictionary(item => item.Id, item => item.Index, StringComparer.Ordinal);
    }

    private string DisplayLiteralValue(
        ResourceLiteralField field,
        string preferredLanguage) =>
        string.IsNullOrWhiteSpace(field.Value)
            ? field.Value
            : _ontology.EnumerationLabel(
                field.Predicate,
                field.Value,
                preferredLanguage) ?? field.Value;

    private string PropertyLabel(string predicate, string preferredLanguage) =>
        _ontology.LabelOf(predicate, preferredLanguage) ?? predicate;

    private static int PropertyPosition(
        IReadOnlyDictionary<string, int> order,
        string predicate) =>
        order.TryGetValue(predicate, out int position)
            ? position
            : int.MaxValue;
}
