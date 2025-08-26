namespace Sql.Baseline.Api.Middleware;

public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx)
    {
        var cid = ctx.Request.Headers[HeaderName].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        ctx.Items[HeaderName] = cid;
        ctx.Response.Headers[HeaderName] = cid;
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", cid))
            await _next(ctx);
    }
}
