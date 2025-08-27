using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Serilog.Events;
using Sql.Baseline.Api.Middleware;

namespace Sql.Baseline.Api.Infrastructure.Startup;

public static class PipelineRegistration
{
    public static WebApplication UseBaselinePipeline(this WebApplication app)
    {
        app.MapPrometheusScrapingEndpoint("/metrics");

        // Forwarded headers first so ClientIP is correct
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        // correlation + custom enrichers + body capture BEFORE Serilog logging
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestLoggingEnricherMiddleware>();
        app.UseMiddleware<RequestResponseBodyLoggingMiddleware>();


        // Serilog request logging (with EnrichDiagnosticContext wired above)
        app.UseSerilogRequestLogging(opts =>
        {
            // ❶ Enrich the Serilog HTTP event with what we stashed in HttpContext.Items
            opts.EnrichDiagnosticContext = (diag, http) =>
            {
                diag.Set("ClientIP", http.Connection.RemoteIpAddress?.ToString());
                diag.Set("UserName", http.User?.Identity?.Name ?? "anonymous");

                if (http.Items.TryGetValue(RequestResponseBodyLoggingMiddleware.RequestBodyKey, out var rb))
                    diag.Set("RequestBody", rb);

                if (http.Items.TryGetValue(RequestResponseBodyLoggingMiddleware.ResponseBodyKey, out var resp))
                    diag.Set("ResponseBody", resp);
            };

            // ❷ Drop noisy endpoints by lowering their log level below the sink’s minimum
            opts.GetLevel = (http, elapsed, ex) =>
            {
                var p = http.Request.Path.Value ?? "";
                if (p.StartsWith("/metrics") || p.StartsWith("/health"))            // silence Prometheus & health checks
                    return LogEventLevel.Debug;                                     // will be suppressed by min level
                return ex is null ? LogEventLevel.Information : LogEventLevel.Error;
            };
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                var provider = app.Services.GetRequiredService<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();
                foreach (var desc in provider.ApiVersionDescriptions)
                    c.SwaggerEndpoint($"/swagger/{desc.GroupName}/swagger.json", $"Baseline API {desc.GroupName}");
            });
        }

        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerPathFeature>();
                var ex = feature?.Error;
                await Results.Problem(title: "Unhandled exception", detail: ex?.Message, statusCode: 500)
                    .ExecuteAsync(context);
            });
        });

        app.UseHttpsRedirection();
        app.UseCors("default");
        app.UseRateLimiter();
        app.UseOutputCache();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready");

        return app;
    }
}