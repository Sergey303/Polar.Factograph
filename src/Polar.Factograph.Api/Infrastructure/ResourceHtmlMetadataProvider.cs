using Microsoft.AspNetCore.Http.Features;
using Polar.Factograph.Api.Documents;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Infrastructure;

public sealed record ResourceHtmlMetadata(
    string Title,
    string Description,
    string SiteName,
    string CanonicalUrl,
    string? ImageUrl = null);

public sealed class ResourceHtmlMetadataProvider(
    ProjectRequestContextFactory contextFactory,
    CassetteDocumentPathResolver? documentResolver = null,
    DocumentContentTypeResolver? contentTypes = null)
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
        string? imageUrl = documentResolver is null || contentTypes is null
            ? null
            : ImageUrl(
                context.Request,
                page,
                readContext.Project,
                readContext.Access,
                documentResolver,
                contentTypes);
        return new ResourceHtmlMetadata(
            title,
            DescriptionOf(page),
            siteName,
            CanonicalUrl(context.Request, page.Portrait.ResourceId),
            imageUrl);
    }

    public static string? TryGetPublicResourceId(HttpRequest request)
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

    public static string TitleOf(PresentedSemanticResourcePage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (DocumentUris(page.Portrait).Any())
        {
            return page.Portrait.TypeLabel
                ?? page.Portrait.Type
                ?? "Документ";
        }

        PresentedResourceLiteralField? named = page.Portrait.Literals.FirstOrDefault(field =>
            IsTerminalPredicate(field.Predicate, "name") ||
            IsTerminalPredicate(field.Predicate, "alias"));
        return string.IsNullOrWhiteSpace(named?.DisplayValue)
            ? page.Portrait.ResourceId
            : named.DisplayValue;
    }

    public static string DescriptionOf(PresentedSemanticResourcePage page)
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

    public static string CanonicalUrl(HttpRequest request, string resourceId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        string pathBase = request.PathBase.HasValue
            ? request.PathBase.Value!.TrimEnd('/')
            : string.Empty;
        return $"{request.Scheme}://{request.Host}{pathBase}/resource/{Uri.EscapeDataString(resourceId)}";
    }

    public static string? ImageUrl(
        HttpRequest request,
        PresentedSemanticResourcePage page,
        ProjectDefinition project,
        ProjectAccessSnapshot access,
        CassetteDocumentPathResolver resolver,
        DocumentContentTypeResolver contentTypes)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(access);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(contentTypes);

        string[] documentUris = DocumentUris(page.Portrait).ToArray();
        if (documentUris.Length != 1)
        {
            return null;
        }

        string documentUri = documentUris[0];
        CassetteDocumentLocation location;
        try
        {
            location = resolver.Resolve(project, documentUri);
        }
        catch (Exception exception) when (
            exception is InvalidDataException or KeyNotFoundException)
        {
            return null;
        }

        if (!access.ReadableCassetteIds.Contains(location.CassetteId) ||
            DocumentImageSelector.Select(location, contentTypes) is null)
        {
            return null;
        }

        return DocumentImageUrl(request, documentUri);
    }

    private static IEnumerable<string> DocumentUris(PresentedProjectResourcePortrait portrait) =>
        portrait.Literals
            .Select(field => field.Value.Trim())
            .Where(CassetteDocumentPathResolver.IsDocumentUri)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);

    private static string DocumentImageUrl(HttpRequest request, string documentUri)
    {
        string pathBase = request.PathBase.HasValue
            ? request.PathBase.Value!.TrimEnd('/')
            : string.Empty;
        return $"{request.Scheme}://{request.Host}{pathBase}/api/documents/image" +
            $"?uri={Uri.EscapeDataString(documentUri)}";
    }

    private static bool IsTerminalPredicate(string predicate, string name) =>
        predicate.EndsWith($"/{name}", StringComparison.OrdinalIgnoreCase) ||
        predicate.EndsWith($"#{name}", StringComparison.OrdinalIgnoreCase);
}
