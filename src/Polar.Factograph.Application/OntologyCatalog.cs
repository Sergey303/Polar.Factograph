namespace Polar.Factograph.Application;

public sealed class OntologyCatalog
{
    private readonly IReadOnlyDictionary<string, OntologyTerm> _terms;
    private readonly OntologyClassHierarchy _hierarchy;
    private readonly OntologyPropertySelector _properties;
    private readonly OntologyEnumerationLabelResolver _enumerations;

    internal OntologyCatalog(IReadOnlyDictionary<string, OntologyTerm> terms)
    {
        _terms = terms ?? throw new ArgumentNullException(nameof(terms));
        _hierarchy = new OntologyClassHierarchy(_terms);
        _properties = new OntologyPropertySelector(_terms, _hierarchy);
        _enumerations = new OntologyEnumerationLabelResolver(_terms);
        _hierarchy.Validate();
    }

    public IReadOnlyCollection<OntologyTerm> Terms => _terms.Values.ToArray();

    public bool TryGetTerm(string id, out OntologyTerm? term)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return _terms.TryGetValue(id, out term);
    }

    public string? LabelOf(string id, string preferredLanguage = "ru") =>
        _terms.TryGetValue(id, out OntologyTerm? term)
            ? OntologyLocalization.Select(term.Labels, preferredLanguage)
            : null;

    public string? InverseLabelOf(string id, string preferredLanguage = "ru") =>
        _terms.TryGetValue(id, out OntologyTerm? term)
            ? OntologyLocalization.Select(term.InverseLabels, preferredLanguage)
            : null;

    public IReadOnlyList<string> AncestorsAndSelf(string classId) =>
        _hierarchy.AncestorsAndSelf(classId);

    public IReadOnlyList<OntologyTerm> DirectPropertiesForType(string classId) =>
        _properties.DirectForType(classId);

    public IReadOnlyList<OntologyTerm> InversePropertiesForType(string classId) =>
        _properties.InverseForType(classId);

    public string? EnumerationLabel(
        string propertyId,
        string value,
        string preferredLanguage = "ru") =>
        _enumerations.Resolve(propertyId, value, preferredLanguage);
}
