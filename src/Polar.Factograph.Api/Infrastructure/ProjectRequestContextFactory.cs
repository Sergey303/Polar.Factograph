using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectRequestContextFactory(
    ProjectPathResolver projectPathResolver,
    ProjectConfigurationLoader projectLoader,
    CurrentUserResolver userResolver,
    ProjectAccessService accessService,
    ProjectStoreProvider storeProvider)
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
        ProjectResourcePortraitService portraits = new(store);
        ProjectResourceSearchService search = new(store, store);
        AuthorizedProjectReadService reads = new(portraits, search);

        return new ProjectReadContext(
            context.Project,
            context.Access,
            store,
            reads);
    }
}
