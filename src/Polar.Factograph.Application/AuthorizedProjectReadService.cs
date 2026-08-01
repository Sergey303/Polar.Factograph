namespace Polar.Factograph.Application;

/// <summary>
/// Security boundary used by future API endpoints. It derives cassette visibility from a validated access snapshot.
/// </summary>
public sealed class AuthorizedProjectReadService
{
    private readonly ProjectResourcePortraitService _portraitService;
    private readonly ProjectResourceSearchService _searchService;

    public AuthorizedProjectReadService(
        ProjectResourcePortraitService portraitService,
        ProjectResourceSearchService searchService)
    {
        _portraitService = portraitService ?? throw new ArgumentNullException(nameof(portraitService));
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
    }

    public ValueTask<ProjectResourcePortrait?> GetPortraitAsync(
        string resourceId,
        ProjectAccessSnapshot access,
        CancellationToken cancellationToken = default) =>
        _portraitService.GetAsync(
            resourceId,
            ProjectAuthorization.RequireRead(access),
            cancellationToken);

    public ValueTask<ProjectResourcePortrait?> GetPortraitSummaryAsync(
        string resourceId,
        ProjectAccessSnapshot access,
        CancellationToken cancellationToken = default) =>
        _portraitService.GetSummaryAsync(
            resourceId,
            ProjectAuthorization.RequireRead(access),
            cancellationToken);

    public Task<IReadOnlyList<ProjectResourceSearchResult>> SearchByNameAsync(
        string query,
        ProjectAccessSnapshot access,
        int limit = 50,
        string preferredLanguage = "ru",
        CancellationToken cancellationToken = default) =>
        _searchService.SearchByNameAsync(
            query,
            ProjectAuthorization.RequireSearch(access),
            limit,
            preferredLanguage,
            cancellationToken);

    public Task<IReadOnlyList<ProjectResourceSearchResult>> SearchByWordsAsync(
        string query,
        ProjectAccessSnapshot access,
        int limit = 50,
        string preferredLanguage = "ru",
        CancellationToken cancellationToken = default) =>
        _searchService.SearchByWordsAsync(
            query,
            ProjectAuthorization.RequireSearch(access),
            limit,
            preferredLanguage,
            cancellationToken);
}
