using Polar.Factograph.Api.Writes;
using Polar.Factograph.Application;
using Polar.Factograph.Fog;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectResourceTargetValidationService(
    ProjectStoreProvider storeProvider,
    OntologyCatalogProvider ontologyProvider,
    OntologyObjectTargetValidator validator)
{
    public async Task ValidateAsync(
        ProjectAccessContext context,
        FogResourceWriteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        if (!HasObjectProperties(request))
        {
            return;
        }

        OntologyCatalog ontology = await ontologyProvider.GetAsync(
            context.Project.Ontology.Path,
            cancellationToken);
        PolarDbTypedProjectStore? store = RequiresCurrentStore(request)
            ? storeProvider.GetCurrent(context.Project.Index.Path)
            : null;
        await validator.ValidateAsync(
            ontology,
            store,
            request,
            context.Access.ReadableCassetteIds,
            cancellationToken);
    }

    private static bool HasObjectProperties(FogResourceWriteRequest request) =>
        request.Properties.Any(property => property.Kind == FogPropertyKind.Resource);

    private static bool RequiresCurrentStore(FogResourceWriteRequest request)
    {
        if (request.ResourceId is null)
        {
            return true;
        }

        string resourceId = FogIdentifier.Clean(request.ResourceId);
        return request.Properties
            .Where(property => property.Kind == FogPropertyKind.Resource)
            .Any(property => !string.Equals(
                FogIdentifier.Clean(property.Value),
                resourceId,
                StringComparison.Ordinal));
    }
}
