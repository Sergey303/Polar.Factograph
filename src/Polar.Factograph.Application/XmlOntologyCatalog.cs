using System.Xml;
using System.Xml.Linq;

namespace Polar.Factograph.Application;

public enum OntologyTermKind
{
    Class = 1,
    DatatypeProperty = 2,
    ObjectProperty = 3,
    EnumerationType = 4
}

public sealed record OntologyLocalizedText(
    string Value,
    string? Language);

public sealed record OntologyEnumerationState(
    string Value,
    string Label,
    string? Language);

public sealed record OntologyTerm(
    string Id,
    OntologyTermKind Kind,
    IReadOnlyList<OntologyLocalizedText> Labels,
    IReadOnlyList<OntologyLocalizedText> InverseLabels,
    string? Priority,
    string? ParentClassId,
    IReadOnlyList<string> Domains,
    IReadOnlyList<string> Ranges,
    IReadOnlyList<OntologyEnumerationState> EnumerationStates);

public sealed class OntologyCatalog
{
    private readonly IReadOnlyDictionary<string, OntologyTerm> _terms;

    internal OntologyCatalog(IReadOnlyDictionary<string, OntologyTerm> terms)
    {
        _terms = terms ?? throw new ArgumentNullException(nameof(terms));
        ValidateClassHierarchy();
    }

    public IReadOnlyCollection<OntologyTerm> Terms => _terms.Values.ToArray();

    public bool TryGetTerm(string id, out OntologyTerm? term)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return _terms.TryGetValue(id, out term);
    }

    public string? LabelOf(string id, string preferredLanguage = "ru") =>
        _terms.TryGetValue(id, out OntologyTerm? term)
            ? SelectLocalized(term.Labels, preferredLanguage)
            : null;

    public string? InverseLabelOf(string id, string preferredLanguage = "ru") =>
        _terms.TryGetValue(id, out OntologyTerm? term)
            ? SelectLocalized(term.InverseLabels, preferredLanguage)
            : null;

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

    public IReadOnlyList<OntologyTerm> DirectPropertiesForType(string classId)
    {
        HashSet<string> ancestors = new(AncestorsAndSelf(classId), StringComparer.Ordinal);
        return _terms.Values
            .Where(term =>
                term.Kind is OntologyTermKind.DatatypeProperty or OntologyTermKind.ObjectProperty &&
                term.Domains.Any(ancestors.Contains))
            .OrderBy(term => term.Priority is null)
            .ThenBy(term => term.Priority, StringComparer.Ordinal)
            .ThenBy(term => term.Id, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<OntologyTerm> InversePropertiesForType(string classId)
    {
        HashSet<string> ancestors = new(AncestorsAndSelf(classId), StringComparer.Ordinal);
        return _terms.Values
            .Where(term =>
                term.Kind == OntologyTermKind.ObjectProperty &&
                term.Ranges.Any(ancestors.Contains))
            .OrderBy(term => term.Priority is null)
            .ThenBy(term => term.Priority, StringComparer.Ordinal)
            .ThenBy(term => term.Id, StringComparer.Ordinal)
            .ToArray();
    }

    public string? EnumerationLabel(
        string propertyId,
        string value,
        string preferredLanguage = "ru")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (!_terms.TryGetValue(propertyId, out OntologyTerm? property))
        {
            return null;
        }

        OntologyTerm? enumeration = property.Ranges
            .Select(range => _terms.TryGetValue(range, out OntologyTerm? term) ? term : null)
            .FirstOrDefault(term => term?.Kind == OntologyTermKind.EnumerationType);
        if (enumeration is null)
        {
            return null;
        }

        OntologyLocalizedText[] labels = enumeration.EnumerationStates
            .Where(state => string.Equals(state.Value, value, StringComparison.Ordinal))
            .Select(state => new OntologyLocalizedText(state.Label, state.Language))
            .ToArray();

        return SelectLocalized(labels, preferredLanguage);
    }

    private OntologyTerm RequireClass(string id)
    {
        if (!_terms.TryGetValue(id, out OntologyTerm? term) || term.Kind != OntologyTermKind.Class)
        {
            throw new KeyNotFoundException($"Ontology class was not found: {id}");
        }

        return term;
    }

    private void ValidateClassHierarchy()
    {
        foreach (OntologyTerm term in _terms.Values.Where(term => term.Kind == OntologyTermKind.Class))
        {
            _ = AncestorsAndSelf(term.Id);
        }
    }

    private static string? SelectLocalized(
        IReadOnlyList<OntologyLocalizedText> values,
        string preferredLanguage)
    {
        if (values.Count == 0)
        {
            return null;
        }

        return values.FirstOrDefault(value =>
                   string.Equals(value.Language, preferredLanguage, StringComparison.OrdinalIgnoreCase))?.Value
               ?? values.FirstOrDefault(value => value.Language is null)?.Value
               ?? values.FirstOrDefault(value =>
                   string.Equals(value.Language, "ru", StringComparison.OrdinalIgnoreCase))?.Value
               ?? values.FirstOrDefault(value =>
                   string.Equals(value.Language, "en", StringComparison.OrdinalIgnoreCase))?.Value
               ?? values[0].Value;
    }
}

public sealed class XmlOntologyCatalogLoader
{
    private static readonly XNamespace Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    private static readonly XNamespace Xml = "http://www.w3.org/XML/1998/namespace";
    private static readonly XmlReaderSettings ReaderSettings = new()
    {
        Async = true,
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreWhitespace = true,
        CloseInput = true
    };

    public async Task<OntologyCatalog> LoadAsync(
        string ontologyPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ontologyPath);

        string fullPath = Path.GetFullPath(ontologyPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Ontology file was not found: {fullPath}", fullPath);
        }

        try
        {
            FileStream stream = new(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 16 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            using XmlReader reader = XmlReader.Create(stream, ReaderSettings);
            XDocument document = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
            XElement root = document.Root
                ?? throw new InvalidDataException($"Ontology XML has no root element: {fullPath}");

            Dictionary<string, OntologyTerm> terms = new(StringComparer.Ordinal);
            foreach (XElement element in root.Elements())
            {
                OntologyTermKind? kind = ParseKind(element.Name.LocalName);
                if (kind is null)
                {
                    continue;
                }

                string id = element.Attribute(Rdf + "about")?.Value
                    ?? throw new InvalidDataException(
                        $"Ontology {element.Name.LocalName} has no rdf:about: {fullPath}");

                OntologyTerm term = new(
                    id,
                    kind.Value,
                    ReadLocalized(element, "label"),
                    ReadLocalized(element, "inverse-label"),
                    element.Attribute("priority")?.Value,
                    element.Elements()
                        .FirstOrDefault(child =>
                            string.Equals(child.Name.LocalName, "SubClassOf", StringComparison.Ordinal))
                        ?.Attribute(Rdf + "resource")
                        ?.Value,
                    ReadResources(element, "domain"),
                    ReadResources(element, "range"),
                    ReadEnumerationStates(element));

                if (!terms.TryAdd(id, term))
                {
                    throw new InvalidDataException($"Duplicate ontology identifier '{id}': {fullPath}");
                }
            }

            return new OntologyCatalog(terms);
        }
        catch (XmlException exception)
        {
            throw new InvalidDataException($"Ontology XML cannot be read: {fullPath}", exception);
        }
    }

    private static OntologyTermKind? ParseKind(string localName) => localName switch
    {
        "Class" => OntologyTermKind.Class,
        "DatatypeProperty" => OntologyTermKind.DatatypeProperty,
        "ObjectProperty" => OntologyTermKind.ObjectProperty,
        "EnumerationType" => OntologyTermKind.EnumerationType,
        _ => null
    };

    private static OntologyLocalizedText[] ReadLocalized(
        XElement element,
        string localName) => element.Elements()
        .Where(child => string.Equals(child.Name.LocalName, localName, StringComparison.Ordinal))
        .Select(child => new OntologyLocalizedText(
            child.Value,
            child.Attribute(Xml + "lang")?.Value))
        .ToArray();

    private static string[] ReadResources(
        XElement element,
        string localName) => element.Elements()
        .Where(child => string.Equals(child.Name.LocalName, localName, StringComparison.Ordinal))
        .Select(child => child.Attribute(Rdf + "resource")?.Value)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value!)
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    private static OntologyEnumerationState[] ReadEnumerationStates(XElement element) =>
        element.Elements()
            .Where(child => string.Equals(child.Name.LocalName, "state", StringComparison.Ordinal))
            .Select(child => new
            {
                Value = child.Attribute("value")?.Value,
                Label = child.Value,
                Language = child.Attribute(Xml + "lang")?.Value
            })
            .Where(state => !string.IsNullOrWhiteSpace(state.Value))
            .Select(state => new OntologyEnumerationState(
                state.Value!,
                state.Label,
                state.Language))
            .ToArray();
}