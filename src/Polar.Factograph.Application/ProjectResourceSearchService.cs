using Polar.Factograph.Storage;

namespace Polar.Factograph.Application;

/// <summary>
/// Public facade for legacy-compatible project name and word searches.
/// </summary>
public sealed class ProjectResourceSearchService
{
    private readonly ProjectNameSearchExecutor _nameSearch;
    private readonly ProjectWordSearchExecutor _wordSearch;

    public ProjectResourceSearchService(
        IProjectSearchStore searchStore,
        IProjectRdfStore rdfStore,
        OntologyCatalog? ontology = null)
    {
        ArgumentNullException.ThrowIfNull(searchStore);
        ArgumentNullException.ThrowIfNull(rdfStore);

        ProjectSearchResultEnricher enricher = new(rdfStore, ontology);
        _nameSearch = new ProjectNameSearchExecutor(searchStore, enricher);
        _wordSearch = new ProjectWordSearchExecutor(searchStore, enricher);
    }

    public Task<IReadOnlyList<ProjectResourceSearchResult>> SearchByNameAsync(
        string query,
        IReadOnlySet<string> allowedCassetteIds,
        int limit = 50,
        string preferredLanguage = "ru",
        CancellationToken cancellationToken = default) =>
        _nameSearch.SearchAsync(
            query,
            allowedCassetteIds,
            limit,
            preferredLanguage,
            cancellationToken);

    public Task<IReadOnlyList<ProjectResourceSearchResult>> SearchByWordsAsync(
        string query,
        IReadOnlySet<string> allowedCassetteIds,
        int limit = 50,
        string preferredLanguage = "ru",
        CancellationToken cancellationToken = default) =>
        _wordSearch.SearchAsync(
            query,
            allowedCassetteIds,
            limit,
            preferredLanguage,
            cancellationToken);
}
