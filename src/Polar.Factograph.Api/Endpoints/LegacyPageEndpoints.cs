namespace Polar.Factograph.Api.Endpoints;

public static class LegacyPageEndpoints
{
    public static IEndpointRouteBuilder MapLegacyPageEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/default.aspx", RedirectLegacyDefault);
        return endpoints;
    }

    public static IResult RedirectLegacyDefault(
        HttpContext context,
        string? id)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(id))
        {
            return Results.Redirect(
                ApplicationLocation(context.Request.PathBase, "/search"),
                permanent: false);
        }

        string encodedId = Uri.EscapeDataString(id.Trim());
        return Results.Redirect(
            ApplicationLocation(
                context.Request.PathBase,
                $"/resource/{encodedId}"),
            permanent: true);
    }

    public static string ApplicationLocation(PathString pathBase, string route)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(route);
        string normalizedRoute = route.StartsWith('/') ? route : $"/{route}";
        string prefix = pathBase.HasValue
            ? pathBase.Value!.TrimEnd('/')
            : string.Empty;
        return prefix + normalizedRoute;
    }
}
