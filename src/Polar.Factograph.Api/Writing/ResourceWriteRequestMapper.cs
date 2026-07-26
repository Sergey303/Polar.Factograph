using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Writing;

public sealed class ResourceWriteRequestMapper
{
    public ProjectResourceWriteCommand Map(
        ResourceWriteBody body,
        string? resourceId = null)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentException.ThrowIfNullOrWhiteSpace(body.TypeId);
        if (body.Properties is null)
        {
            throw new ArgumentException("Resource properties are required.", nameof(body));
        }

        FogProperty[] properties = body.Properties
            .Select(MapProperty)
            .ToArray();
        FogResourceWriteRequest resource = new(
            body.TypeId,
            properties,
            string.IsNullOrWhiteSpace(resourceId) ? null : resourceId);
        return new ProjectResourceWriteCommand(
            resource,
            string.IsNullOrWhiteSpace(body.CassetteId) ? null : body.CassetteId);
    }

    private static FogProperty MapProperty(ResourcePropertyWriteBody property)
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentException.ThrowIfNullOrWhiteSpace(property.Predicate);
        ArgumentException.ThrowIfNullOrWhiteSpace(property.Kind);
        if (property.Value is null)
        {
            throw new ArgumentException("Resource property value is required.");
        }

        FogPropertyKind kind = property.Kind.Trim().ToLowerInvariant() switch
        {
            "literal" => FogPropertyKind.Literal,
            "resource" => FogPropertyKind.Resource,
            _ => throw new ArgumentException(
                $"Unknown resource property kind: {property.Kind}")
        };
        return new FogProperty(
            property.Predicate,
            kind,
            property.Value,
            property.Language,
            property.DataType);
    }
}
