using System.Text;
using System.Text.Encodings.Web;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class DynamicBaseUrlMiddleware(RequestDelegate next)
{
    private const string ApplicationBaseMetaName = "factograph-app-base";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldInspect(context.Request))
        {
            await next(context);
            return;
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
