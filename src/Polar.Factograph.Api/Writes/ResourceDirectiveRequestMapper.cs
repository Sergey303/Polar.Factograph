using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Writes;

public static class ResourceDirectiveRequestMapper
{
    private const int MaxIdentifierLength = 2_048;

    public static FogDirectiveWriteRequest Map(ResourceDeleteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireIdentifier(request.ResourceId, nameof(request.ResourceId));
        return new FogDirectiveWriteRequest(
            FogRecordKind.Delete,
            request.ResourceId);
    }

    public static FogDirectiveWriteRequest Map(ResourceSubstituteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireIdentifier(request.OldResourceId, nameof(request.OldResourceId));
        RequireIdentifier(request.NewResourceId, nameof(request.NewResourceId));
        return new FogDirectiveWriteRequest(
            FogRecordKind.Substitute,
            request.OldResourceId,
            request.NewResourceId);
    }

    private static void RequireIdentifier(string value, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        if (value.Length > MaxIdentifierLength)
        {
            throw new ArgumentException(
                $"{name} must not exceed {MaxIdentifierLength} characters.",
                name);
        }
    }
}
