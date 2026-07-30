using Polar.Factograph.Application;

namespace Polar.Factograph.Api.Endpoints;

public sealed record SearchEvidenceResponse(string Value);

public sealed record ResourceSearchResponse(
    string ResourceId,
    string DisplayName,
    string? Type,
    string? TypeLabel,
    int Score,
    IReadOnlyList<SearchEvidenceResponse> Matches);

public sealed record ResourceTypeSearchPageResponse(
    string ClassId,
    string Label,
    int Total,
    int Offset,
    int Limit,
    IReadOnlyList<ResourceSearchResponse> Results);

public static class SearchResponsePresentation
{
    public static ResourceSearchResponse Present(ProjectResourceSearchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        SearchEvidenceResponse[] matches = result.Matches
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal)
            .Select(value => new SearchEvidenceResponse(value))
            .ToArray();
        return new ResourceSearchResponse(
            result.ResourceId,
            result.DisplayName,
            result.Type,
            result.TypeLabel,
            result.Score,
            matches);
    }

    public static ResourceTypeSearchPageResponse Present(
        ProjectResourceTypeSearchPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return new ResourceTypeSearchPageResponse(
            page.ClassId,
            page.Label,
            page.Total,
            page.Offset,
            page.Limit,
            page.Results.Select(Present).ToArray());
    }
}
