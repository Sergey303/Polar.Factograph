using Microsoft.AspNetCore.Http.Features;
using Polar.Factograph.Application;

namespace Polar.Factograph.Api.Infrastructure;

public sealed record ResourceHtmlMetadata(
    string Title,
    string Description,
    string SiteName,
    string CanonicalUrl);

public sealed class ResourceHtmlMetadataProvider(
    ProjectRequestContextFactory contextFactory)
{
    private const int DescriptionLimit = 240;

    public async Task<ResourceHtmlMetadata?> TryGetAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        string? resourceId = TryGetPublicResourceId(context.Request);
        if (resourceId is null)
        {
            return null;
        }

        ProjectReadContext readContext = await contextFactory.CreateReadAsync(
            context,
            cancellationToken);
        PresentedSemanticResourcePage? page = await readContext.SemanticPages.GetAsync(
            resourceId,
            readContext.Access,
            "ru",
            cancellationToken);
        if (page is null)
        {
            return null;
        }

        string title = TitleOf(page);
        string siteName = readContext.Project.Name;
        return new ResourceHtmlMetadata(
            title,
            DescriptionOf(page),
            siteName,
            CanonicalUrl(context.Request, page.Portrait.ResourceId));
    }

    internal static string? TryGetPublicResourceId(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        string rawTarget = request.HttpContext.Features
            .Get<IHttpRequestFeature>()
            ?.RawTarget
            ?? request.PathBase.Add(request.Path).Value
            ?? string.Empty;
        int queryIndex = rawTarget.IndexOf('?');
        string rawPath = queryIndex < 0 ? rawTarget : rawTarget[..queryIndex];
        string pathBase = request.PathBase.HasValue
            ? request.PathBase.Value!
            : string.Empty;
        if (pathBase.Length > 0 && rawPath.StartsWith(pathBase, StringComparison.OrdinalIgnoreCase))
        {
            rawPath = rawPath[pathBase.Length..];
        }

        const string prefix = "/resource/";
        if (!rawPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string encoded = rawPath[prefix.Length..];
        if (encoded.Length == 0 || encoded.Contains('/'))
        {
            return null;
        }

        try
        {
            string resourceId = Uri.UnescapeDataString(encoded).Trim();
            return resourceId.Length == 0 ? null : resourceId;
        }
        catch (UriFormatException)
        {
            return null;
        }
    }

    internal static string TitleOf(PresentedSemanticResourcePage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        PresentedResourceLiteralField? named = page.Portrait.Literals.FirstOrDefault(field =>
            IsTerminalPredicate(field.Predicate, "name") ||
            IsTerminalPredicate(field.Predicate, "alias"));
        return string.IsNullOrWhiteSpace(named?.DisplayValue)
            ? page.Portrait.ResourceId
            : named.DisplayValue;
    }

    internal static string DescriptionOf(PresentedSemanticResourcePage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        PresentedResourceLiteralField? descriptive = page.Portrait.Literals.FirstOrDefault(field =>
            (IsTerminalPredicate(field.Predicate, "description") ||
             IsTerminalPredicate(field.Predicate, "comment")) &&
            !string.IsNullOrWhiteSpace(field.DisplayValue));
        string value = descriptive?.DisplayValue.Trim()
            ?? page.Portrait.TypeLabel
            ?? page.Portrait.Type
            ?? "Ресурс";
        return value.Length <= DescriptionLimit
            ? value
            : $"{value[..(DescriptionLimit - 3)].TrimEnd()}…";
    }

    internal static string CanonicalUrl(HttpRequest request, string resourceId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        string pathBase = request.PathBase.HasValue
            ? request.PathBase.Value!.TrimEnd('/')
            : string.Empty;
        return $"{request.Scheme}://{request.Host}{pathBase}/resource/{Uri.EscapeDataString(resourceId)}";
    }

    private static bool IsTerminalPredicate(string predicate, string name) =>
        predicate.EndsWith($"/{name}", StringComparison.OrdinalIgnoreCase) ||
        predicate.EndsWith($"#{name}", StringComparison.OrdinalIgnoreCase);
}
