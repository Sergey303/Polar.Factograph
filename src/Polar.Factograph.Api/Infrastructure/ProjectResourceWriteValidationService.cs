using Polar.Factograph.Api.Writes;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectResourceWriteValidationService(
    OntologyCatalogProvider ontologyProvider,
    OntologyResourceWriteValidator validator)
{
    public async Task ValidateAsync(
        ProjectDefinition project,
        FogResourceWriteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(request);
        OntologyCatalog ontology = await ontologyProvider.GetAsync(
            project.Ontology.Path,
            cancellationToken);
        validator.Validate(ontology, request);
    }
}
