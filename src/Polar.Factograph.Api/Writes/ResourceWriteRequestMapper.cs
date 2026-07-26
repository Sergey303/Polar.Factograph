using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Writes;

public static class ResourceWriteRequestMapper
{
    public static FogResourceWriteRequest Map(ResourceWriteRequest request)
    {
        ResourceWriteRequestValidator.Validate(request);
        FogProperty[] properties = request.Properties
            .Select(MapProperty)
            .ToArray();

        return new FogResourceWriteRequest(
            request.TypeId,
            properties,
            request.ResourceId);
    }

    private static FogProperty MapProperty(ResourceWritePropertyRequest property) => new(
        property.Predicate,
        string.Equals(property.Kind, "resource", StringComparison.OrdinalIgnoreCase)
            ? FogPropertyKind.Resource
            : FogPropertyKind.Literal,
        property.Value,
        property.Language,
        property.DataType);
}
