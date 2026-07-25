namespace Polar.Factograph.Application;

public sealed class AuthorizedProjectCollectionService(ProjectCollectionService collections)
{
    public Task<ProjectCollectionContents?> GetAsync(
        string collectionId,
        ProjectAccessSnapshot access,
        int limit = 100,
        string preferredLanguage = "ru",
        CancellationToken cancellationToken = default) =>
        collections.GetAsync(
            collectionId,
            ProjectAuthorization.RequireRead(access),
            limit,
            preferredLanguage,
            cancellationToken);
}
