using Polar.Factograph.Application;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Writes;

public sealed class OntologyResourceWriteValidator
{
    public void Validate(
        OntologyCatalog catalog,
        FogResourceWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(request);
        OntologyTerm type = OntologyWriteTermResolver.Require(
            catalog,
            request.TypeId,
            "class");
        if (type.Kind != OntologyTermKind.Class)
        {
            throw new ArgumentException(
                $"Ontology term '{type.Id}' is not a class.");
        }

        IReadOnlyDictionary<string, OntologyTerm> allowed = catalog
            .DirectPropertiesForType(type.Id)
            .ToDictionary(term => term.Id, StringComparer.Ordinal);
        foreach (FogProperty property in request.Properties)
        {
            ValidateProperty(catalog, allowed, property, type.Id);
        }
    }

    private static void ValidateProperty(
        OntologyCatalog catalog,
        IReadOnlyDictionary<string, OntologyTerm> allowed,
        FogProperty property,
        string typeId)
    {
        OntologyTerm term = OntologyWriteTermResolver.Require(
            catalog,
            property.Predicate,
            "property");
        if (!allowed.ContainsKey(term.Id))
        {
            throw new ArgumentException(
                $"Ontology property '{term.Id}' is not allowed for class '{typeId}'.");
        }

        OntologyTermKind expected = property.Kind == FogPropertyKind.Resource
            ? OntologyTermKind.ObjectProperty
            : OntologyTermKind.DatatypeProperty;
        if (term.Kind != expected)
        {
            throw new ArgumentException(
                $"Ontology property '{term.Id}' requires {ExpectedValue(expected)} values.");
        }

        if (property.Kind == FogPropertyKind.Resource &&
            (!string.IsNullOrWhiteSpace(property.Language) ||
             !string.IsNullOrWhiteSpace(property.DataType)))
        {
            throw new ArgumentException(
                $"Resource property '{term.Id}' cannot have language or datatype metadata.");
        }

        if (property.Kind == FogPropertyKind.Literal &&
            !string.IsNullOrWhiteSpace(property.DataType) &&
            !term.Ranges.Contains(property.DataType, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"Datatype '{property.DataType}' is not allowed for ontology property '{term.Id}'.");
        }

        OntologyEnumerationWriteValidator.Validate(catalog, term, property);
    }

    private static string ExpectedValue(OntologyTermKind kind) =>
        kind == OntologyTermKind.ObjectProperty ? "resource" : "literal";
}
