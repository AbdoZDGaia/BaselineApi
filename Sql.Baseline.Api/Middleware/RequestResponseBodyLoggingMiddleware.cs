using System.Text;
using System.Text.Json;

namespace Sql.Baseline.Api.Middleware;

public sealed class RequestResponseBodyLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly string[] AllowedTypes = { "application/json", "text/plain", "application/x-www-form-urlencoded" };
    private const int MaxBytes = 8 * 1024;

    public const string RequestBodyKey = "__ReqBody";
    public const string ResponseBodyKey = "__RespBody";

    public RequestResponseBodyLoggingMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx)
    {
        // REQUEST
        if (ctx.Request.ContentLength is > 0 && IsAllowed(ctx.Request.ContentType))
        {
            ctx.Request.EnableBuffering();
            using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: MaxBytes, leaveOpen: true);
            var raw = await reader.ReadToEndAsync();
            ctx.Request.Body.Position = 0;
            ctx.Items[RequestBodyKey] = Mask(Bound(raw, MaxBytes));
        }

        // RESPONSE (swap stream before next)
        var original = ctx.Response.Body;
        using var capture = new MemoryStream();
        ctx.Response.Body = capture;

        try
        {
            await _next(ctx); // the rest of the pipeline
        }
        finally
        {
            capture.Position = 0;
            string? body = null;
            if (IsAllowed(ctx.Response.ContentType) && capture.Length > 0)
            {
                using var reader = new StreamReader(capture, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                body = Bound(await reader.ReadToEndAsync(), MaxBytes);
                capture.Position = 0;
            }

            ctx.Items[ResponseBodyKey] = body ?? "(none)";
            await capture.CopyToAsync(original);
            ctx.Response.Body = original;
        }
    }

    private static bool IsAllowed(string? ct) =>
        ct != null && AllowedTypes.Any(t => ct.StartsWith(t, StringComparison.OrdinalIgnoreCase));

    private static string Bound(string s, int max) => s.Length <= max ? s : s[..max] + "…[truncated]";

    private static string Mask(string s)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(s);
            if (dict is null) return s;
            foreach (var k in dict.Keys.ToList())
            {
                if (k.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                    k.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                    k.Contains("token", StringComparison.OrdinalIgnoreCase))
                {
                    dict[k] = "***";
                }
            }
            return JsonSerializer.Serialize(dict);
        }
        catch { return s; }
    }
}
