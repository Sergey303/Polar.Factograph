using System.Text;
using System.Text.Encodings.Web;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class DynamicBaseUrlMiddleware(RequestDelegate next)
{
    private const string ApplicationBaseMetaName = "factograph-app-base";

    public async Task InvokeAsync(
        HttpContext context,
        ResourceHtmlMetadataProvider metadataProvider,
        ILogger<DynamicBaseUrlMiddleware> logger)
    {
        if (!ShouldInspect(context.Request))
        {
            await next(context);
            return;
        }

        bool resourceViewRequest =
            ResourceHtmlMetadataProvider.TryGetPublicResourceId(context.Request) is not null;
        if (resourceViewRequest)
        {
            context.Request.Headers.Remove("If-None-Match");
            context.Request.Headers.Remove("If-Modified-Since");
        }

        Stream originalBody = context.Response.Body;
        await using MemoryStream bufferedBody = new();
        context.Response.Body = bufferedBody;
        try
        {
            await next(context);
            if (!IsHtml(context.Response))
            {
                bufferedBody.Position = 0;
                await bufferedBody.CopyToAsync(originalBody, context.RequestAborted);
                return;
            }

            bufferedBody.Position = 0;
            using StreamReader reader = new(
                bufferedBody,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                leaveOpen: true);
            string html = await reader.ReadToEndAsync(context.RequestAborted);
            string rewritten = InsertApplicationBase(html, BaseHref(context.Request.PathBase));
            try
            {
                ResourceHtmlMetadata? metadata = await metadataProvider.TryGetAsync(
                    context,
                    context.RequestAborted);
                if (metadata is not null)
                {
                    rewritten = InsertResourceMetadata(rewritten, metadata);
                    DisableStaticFileCaching(context.Response);
                }
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Could not add resource metadata to HTML response for {Path}.",
                    context.Request.Path);
            }

            byte[] bytes = Encoding.UTF8.GetBytes(rewritten);
            context.Response.ContentLength = bytes.Length;
            await originalBody.WriteAsync(bytes, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    internal static string InsertApplicationBase(string html, string href)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentException.ThrowIfNullOrWhiteSpace(href);

        int head = html.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
        if (head < 0)
        {
            return html;
        }

        string encodedHref = HtmlEncoder.Default.Encode(href);
        StringBuilder tags = new();
        if (!html.Contains("<base ", StringComparison.OrdinalIgnoreCase))
        {
            tags.Append($"<base href=\"{encodedHref}\">");
        }

        if (!html.Contains(ApplicationBaseMetaName, StringComparison.OrdinalIgnoreCase))
        {
            tags.Append(
                $"<meta name=\"{ApplicationBaseMetaName}\" content=\"{encodedHref}\">");
        }

        return tags.Length == 0
            ? html
            : html.Insert(head + "<head>".Length, tags.ToString());
    }

    internal static string InsertResourceMetadata(
        string html,
        ResourceHtmlMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentNullException.ThrowIfNull(metadata);

        int head = html.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
        if (head < 0)
        {
            return html;
        }

        string title = HtmlEncoder.Default.Encode($"{metadata.Title} — {metadata.SiteName}");
        string description = HtmlEncoder.Default.Encode(metadata.Description);
        string pageTitle = HtmlEncoder.Default.Encode(metadata.Title);
        string siteName = HtmlEncoder.Default.Encode(metadata.SiteName);
        string canonical = HtmlEncoder.Default.Encode(metadata.CanonicalUrl);
        string rewritten = ReplaceTitle(html, title);
        head = rewritten.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);

        StringBuilder tags = new();
        tags.Append($"<meta name=\"description\" content=\"{description}\">");
        tags.Append($"<meta property=\"og:title\" content=\"{pageTitle}\">");
        tags.Append($"<meta property=\"og:description\" content=\"{description}\">");
        tags.Append("<meta property=\"og:type\" content=\"website\">");
        tags.Append($"<meta property=\"og:url\" content=\"{canonical}\">");
        tags.Append($"<meta property=\"og:site_name\" content=\"{siteName}\">");
        tags.Append("<meta name=\"twitter:card\" content=\"summary\">");
        tags.Append($"<meta name=\"twitter:title\" content=\"{pageTitle}\">");
        tags.Append($"<meta name=\"twitter:description\" content=\"{description}\">");
        tags.Append($"<link rel=\"canonical\" href=\"{canonical}\">");
        return rewritten.Insert(head + "<head>".Length, tags.ToString());
    }

    private static string ReplaceTitle(string html, string encodedTitle)
    {
        int start = html.IndexOf("<title", StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            int head = html.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
            return head < 0
                ? html
                : html.Insert(head + "<head>".Length, $"<title>{encodedTitle}</title>");
        }

        int openEnd = html.IndexOf('>', start);
        int close = openEnd < 0
            ? -1
            : html.IndexOf("</title>", openEnd + 1, StringComparison.OrdinalIgnoreCase);
        if (openEnd < 0 || close < 0)
        {
            return html;
        }

        return html[..start] +
            $"<title>{encodedTitle}</title>" +
            html[(close + "</title>".Length)..];
    }

    private static void DisableStaticFileCaching(HttpResponse response)
    {
        response.Headers.Remove("ETag");
        response.Headers.Remove("Last-Modified");
        response.Headers["Cache-Control"] = "private, no-store";
    }

    private static bool ShouldInspect(HttpRequest request)
    {
        if (!HttpMethods.IsGet(request.Method) ||
            request.Path.StartsWithSegments("/api"))
        {
            return false;
        }

        return !Path.HasExtension(request.Path.Value) ||
            string.Equals(
                Path.GetExtension(request.Path.Value),
                ".html",
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHtml(HttpResponse response) =>
        response.StatusCode == StatusCodes.Status200OK &&
        response.ContentType?.StartsWith(
            "text/html",
            StringComparison.OrdinalIgnoreCase) == true;

    private static string BaseHref(PathString pathBase)
    {
        string value = pathBase.HasValue
            ? pathBase.Value!.TrimEnd('/')
            : string.Empty;
        return $"{value}/";
    }
}
