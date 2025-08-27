using Serilog;
using Sql.Baseline.Api.Middleware;

namespace Sql.Baseline.Api.Infrastructure.Startup;

public static class PipelineRegistration
{
    public static WebApplication UseBaselinePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestLoggingEnricherMiddleware>();

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

        app.UseExceptionHandler("/error");
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