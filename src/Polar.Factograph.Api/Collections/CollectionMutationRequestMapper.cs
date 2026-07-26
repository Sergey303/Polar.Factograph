using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Collections;

public static class CollectionMutationRequestMapper
{
    private const int MaxIdentifierLength = 2_048;

    public static CollectionItemAddRequest Normalize(CollectionItemAddRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request with
        {
            CollectionId = NormalizeIdentifier(request.CollectionId, nameof(request.CollectionId)),
            ResourceId = NormalizeIdentifier(request.ResourceId, nameof(request.ResourceId))
        };
    }

    public static CollectionItemRemoveRequest Normalize(CollectionItemRemoveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request with
        {
            MembershipResourceId = NormalizeIdentifier(
                request.MembershipResourceId,
                nameof(request.MembershipResourceId)),
            CollectionId = NormalizeIdentifier(request.CollectionId, nameof(request.CollectionId)),
            ResourceId = NormalizeIdentifier(request.ResourceId, nameof(request.ResourceId))
        };
    }

    private static string NormalizeIdentifier(string value, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        if (value.Length > MaxIdentifierLength)
        {
            throw new ArgumentException(
                $"{name} must not exceed {MaxIdentifierLength} characters.",
                name);
        }

        return FogIdentifier.Clean(value);
    }
}
