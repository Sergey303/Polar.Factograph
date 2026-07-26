namespace Polar.Factograph.Api.Writes;

internal static class ResourceWriteRequestValidator
{
    private const int MaxProperties = 1_000;
    private const int MaxIdentifierLength = 2_048;
    private const int MaxLiteralLength = 1_000_000;

    public static void Validate(ResourceWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireText(request.TypeId, nameof(request.TypeId), MaxIdentifierLength);
        if (request.Properties is null || request.Properties.Count > MaxProperties)
        {
            throw new ArgumentException(
                $"Properties must contain at most {MaxProperties} items.",
                nameof(request));
        }

        if (request.ResourceId is not null)
        {
            RequireText(request.ResourceId, nameof(request.ResourceId), MaxIdentifierLength);
        }

        foreach (ResourceWritePropertyRequest property in request.Properties)
        {
            ValidateProperty(property);
        }
    }

    private static void ValidateProperty(ResourceWritePropertyRequest property)
    {
        ArgumentNullException.ThrowIfNull(property);
        RequireText(property.Predicate, nameof(property.Predicate), MaxIdentifierLength);
        if (property.Value is null || property.Value.Length > MaxLiteralLength)
        {
            throw new ArgumentException(
                $"Property value must not exceed {MaxLiteralLength} characters.");
        }

        if (!IsKind(property.Kind, "literal") && !IsKind(property.Kind, "resource"))
        {
            throw new ArgumentException(
                $"Unknown property kind '{property.Kind}'. Use 'literal' or 'resource'.");
        }

        if (IsKind(property.Kind, "resource") && string.IsNullOrWhiteSpace(property.Value))
        {
            throw new ArgumentException("Resource property value cannot be empty.");
        }
    }

    private static bool IsKind(string? actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    private static void RequireText(string value, string name, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        if (value.Length > maxLength)
        {
            throw new ArgumentException(
                $"{name} must not exceed {maxLength} characters.",
                name);
        }
    }
}
