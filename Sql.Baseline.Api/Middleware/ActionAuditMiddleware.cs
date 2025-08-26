using Serilog;

namespace Sql.Baseline.Api.Middleware;

public class ActionAuditMiddleware
{
    private readonly RequestDelegate _next;
    public ActionAuditMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _next(ctx);
        sw.Stop();
        Log.Information("HTTP {Method} {Path} => {Status} in {Elapsed}ms",
            ctx.Request.Method, ctx.Request.Path, ctx.Response.StatusCode, sw.ElapsedMilliseconds);
    }
}
