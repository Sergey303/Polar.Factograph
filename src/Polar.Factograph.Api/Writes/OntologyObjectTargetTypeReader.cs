using Polar.Factograph.Application;
using Polar.Factograph.Fog;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Writes;

internal static class OntologyObjectTargetTypeReader
{
    public static async Task<IReadOnlyList<string>> ReadAsync(
        IProjectRdfStore? store,
        FogResourceWriteRequest request,
        string targetValue,
        IReadOnlySet<string> readableCassetteIds,
        CancellationToken cancellationToken)
    {
        string targetId = FogIdentifier.Clean(targetValue);
        if (IsSelfReference(request, targetId))
        {
            return [request.TypeId];
        }

        if (store is null)
        {
            throw new InvalidOperationException(
                "A current project index is required to validate an external resource target.");
        }

        ResourceHead? head = await store.GetResourceHeadAsync(targetId, cancellationToken);
        if (head is null || head.IsDeleted ||
            !readableCassetteIds.Contains(head.SourceCassetteId))
        {
            throw new ArgumentException(
                $"Resource target '{targetId}' does not exist or is not readable.");
        }

        IReadOnlyList<string> types = await new ProjectResourceTypeReader(store)
            .ReadAllAsync(targetId, readableCassetteIds, cancellationToken);
        return types.Count > 0
            ? types
            : throw new ArgumentException(
                $"Resource target '{targetId}' has no ontology class.");
    }

    private static bool IsSelfReference(
        FogResourceWriteRequest request,
        string targetId) =>
        request.ResourceId is not null &&
        string.Equals(
            FogIdentifier.Clean(request.ResourceId),
            targetId,
            StringComparison.Ordinal);
}
