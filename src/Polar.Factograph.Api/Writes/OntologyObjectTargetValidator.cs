using Polar.Factograph.Application;
using Polar.Factograph.Fog;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Writes;

public sealed class OntologyObjectTargetValidator
{
    public async Task ValidateAsync(
        OntologyCatalog catalog,
        IProjectRdfStore? store,
        FogResourceWriteRequest request,
        IReadOnlySet<string> readableCassetteIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(readableCassetteIds);

        foreach (FogProperty property in request.Properties
                     .Where(item => item.Kind == FogPropertyKind.Resource))
        {
            OntologyTerm term = OntologyWriteTermResolver.Require(
                catalog,
                property.Predicate,
                "property");
            IReadOnlyList<string> targetTypes = await OntologyObjectTargetTypeReader.ReadAsync(
                store,
                request,
                property.Value,
                readableCassetteIds,
                cancellationToken);
            if (!OntologyObjectRangeMatcher.Matches(catalog, term, targetTypes))
            {
                throw new ArgumentException(
                    $"Resource '{FogIdentifier.Clean(property.Value)}' is outside the range of ontology property '{term.Id}'.");
            }
        }
    }
}
