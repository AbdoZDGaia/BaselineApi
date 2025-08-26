namespace Sql.Baseline.Api.Middleware;

public class RequestLoggingEnricherMiddleware
{
    private readonly RequestDelegate _next;
    public RequestLoggingEnricherMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx)
    {
        var user = ctx.User?.Identity?.IsAuthenticated == true ? ctx.User.Identity!.Name : "anonymous";
        using (Serilog.Context.LogContext.PushProperty("UserName", user))
        using (Serilog.Context.LogContext.PushProperty("ClientIP", ctx.Connection.RemoteIpAddress?.ToString()))
        {
            await _next(ctx);
        }
    }
}
