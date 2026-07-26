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
        PolarDbTypedProjectStore store = storeProvider.GetCurrent(
            context.Project.Index.Path);
        OntologyCatalog ontology = await ontologyProvider.GetAsync(
            context.Project.Ontology.Path,
            cancellationToken);
        await validator.ValidateAsync(
            ontology,
            store,
            request,
            context.Access.ReadableCassetteIds,
            cancellationToken);
    }
}
