using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectRequestContextFactory(
    ProjectPathResolver projectPathResolver,
    ProjectConfigurationLoader projectLoader,
    CurrentUserResolver userResolver,
    ProjectAccessService accessService,
    ProjectStoreProvider storeProvider,
    OntologyCatalogProvider ontologyProvider)
{
    public async Task<ProjectAccessContext> CreateAccessAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        string projectPath = projectPathResolver.GetRequiredPath();
        ProjectDefinition project = await projectLoader.LoadAsync(projectPath, cancellationToken);
        string userId = userResolver.Resolve(httpContext);
        ProjectAccessSnapshot access = accessService.Evaluate(project, userId);
        return new ProjectAccessContext(project, access);
    }

    public async Task<ProjectReadContext> CreateReadAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ProjectAccessContext context = await CreateAccessAsync(httpContext, cancellationToken);
        PolarDbTypedProjectStore store = storeProvider.GetCurrent(context.Project.Index.Path);
        OntologyCatalog ontology = await ontologyProvider.GetAsync(
            context.Project.Ontology.Path,
            cancellationToken);
        ProjectResourcePortraitService rawPortraits = new(store);
        ProjectResourceSearchService search = new(store, store, ontology);
        AuthorizedProjectReadService reads = new(rawPortraits, search);
        AuthorizedPresentedPortraitService portraits = new(
            reads,
            new OntologyResourcePortraitPresenter(ontology));
        AuthorizedProjectCollectionService collections = new(
            new ProjectCollectionService(store, store, ontology));

        return new ProjectReadContext(
            context.Project,
            context.Access,
            store,
            reads,
            portraits,
            collections);
    }
}
