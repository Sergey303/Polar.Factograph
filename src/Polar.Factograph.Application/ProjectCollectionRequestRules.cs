namespace Polar.Factograph.Application;

internal static class ProjectCollectionRequestRules
{
    public static void Validate(
        string collectionId,
        IReadOnlySet<string> allowedCassetteIds,
        int limit,
        string preferredLanguage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionId);
        ArgumentNullException.ThrowIfNull(allowedCassetteIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(preferredLanguage);
        if (limit is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                limit,
                "Collection limit must be between 1 and 500.");
        }
    }
}
