using Polar.Factograph.Application;
using Polar.Factograph.Fog;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Writes;

public sealed class OntologyObjectTargetValidator
{
    public async Task ValidateAsync(
        OntologyCatalog catalog,
        IProjectRdfStore store,
        FogResourceWriteRequest request,
        IReadOnlySet<string> readableCassetteIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(readableCassetteIds);
        ProjectResourceTypeReader typeReader = new(store);

        foreach (FogProperty property in request.Properties
                     .Where(item => item.Kind == FogPropertyKind.Resource))
        {
            OntologyTerm term = OntologyWriteTermResolver.Require(
                catalog,
                property.Predicate,
                "property");
            IReadOnlyList<string> targetTypes = await ReadTargetTypesAsync(
                store,
                typeReader,
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

    private static async Task<IReadOnlyList<string>> ReadTargetTypesAsync(
        IProjectRdfStore store,
        ProjectResourceTypeReader typeReader,
        FogResourceWriteRequest request,
        string targetValue,
        IReadOnlySet<string> readableCassetteIds,
        CancellationToken cancellationToken)
    {
        string targetId = FogIdentifier.Clean(targetValue);
        if (request.ResourceId is not null &&
            string.Equals(
                FogIdentifier.Clean(request.ResourceId),
                targetId,
                StringComparison.Ordinal))
        {
            return [request.TypeId];
        }

        ResourceHead? head = await store.GetResourceHeadAsync(targetId, cancellationToken);
        if (head is null || head.IsDeleted ||
            !readableCassetteIds.Contains(head.SourceCassetteId))
        {
            throw new ArgumentException(
                $"Resource target '{targetId}' does not exist or is not readable.");
        }

        IReadOnlyList<string> types = await typeReader.ReadAllAsync(
            targetId,
            readableCassetteIds,
            cancellationToken);
        return types.Count > 0
            ? types
            : throw new ArgumentException(
                $"Resource target '{targetId}' has no ontology class.");
    }
}
