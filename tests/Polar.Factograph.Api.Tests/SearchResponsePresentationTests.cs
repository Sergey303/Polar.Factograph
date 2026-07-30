using Polar.Factograph.Api.Endpoints;
using Polar.Factograph.Application;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class SearchResponsePresentationTests
{
    [Fact]
    public void Present_KeepsOnlyDistinctVisibleEvidenceValues()
    {
        ProjectResourceSearchResult source = new(
            "resource-1",
            "Иван Иванов",
            "person",
            "Персона",
            120,
            "cassette-a",
            [
                new ProjectSearchEvidence("name", "Иван Иванов", "ru"),
                new ProjectSearchEvidence("alias", "Иван Иванов", null),
                new ProjectSearchEvidence("description", "Исследователь", "ru")
            ]);

        ResourceSearchResponse response = SearchResponsePresentation.Present(source);

        Assert.Equal("resource-1", response.ResourceId);
        Assert.Equal("Иван Иванов", response.DisplayName);
        Assert.Equal(120, response.Score);
        Assert.Equal(
            new[] { "Иван Иванов", "Исследователь" },
            response.Matches.Select(match => match.Value));
    }

    [Fact]
    public void Present_MapsTypePageWithoutLeakingInternalSearchRows()
    {
        ProjectResourceTypeSearchPage source = new(
            "person",
            "Персона",
            Total: 1,
            Offset: 0,
            Limit: 50,
            [
                new ProjectResourceSearchResult(
                    "resource-1",
                    "Иван Иванов",
                    "person",
                    "Персона",
                    0,
                    "cassette-a",
                    [])
            ]);

        ResourceTypeSearchPageResponse response = SearchResponsePresentation.Present(source);

        Assert.Equal("person", response.ClassId);
        Assert.Equal(1, response.Total);
        ResourceSearchResponse result = Assert.Single(response.Results);
        Assert.Equal("resource-1", result.ResourceId);
        Assert.Empty(result.Matches);
    }
}
