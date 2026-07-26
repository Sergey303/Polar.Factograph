namespace Polar.Factograph.Api.Collections;

internal static class CollectionMutationResults
{
    public static IResult Added(CollectionItemMutationResponse response)
    {
        string location = ContentsLocation(response.CollectionId);
        return response.IndexReady
            ? Results.Created(location, response)
            : Results.Accepted(location, response);
    }

    public static IResult Removed(CollectionItemMutationResponse response)
    {
        string location = ContentsLocation(response.CollectionId);
        return response.IndexReady
            ? Results.Ok(response)
            : Results.Accepted(location, response);
    }

    private static string ContentsLocation(string collectionId) =>
        $"/api/collections/items?id={Uri.EscapeDataString(collectionId)}";
}
