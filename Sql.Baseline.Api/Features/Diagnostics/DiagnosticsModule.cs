using Asp.Versioning;
using Asp.Versioning.Builder;
using Sql.Baseline.Api.Infrastructure.Endpoints;


namespace Sql.Baseline.Api.Features.Diagnostics;


public sealed class DiagnosticsModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app, ApiVersionSet versions, IConfiguration cfg)
    {
        // Health checks are already added globally in Program.cs; just expose some helpers here
        var group = app.MapGroup("/api/v{version:apiVersion}/diagnostics")
        .WithTags("Diagnostics")
        .WithApiVersionSet(versions)
        .MapToApiVersion(new Asp.Versioning.ApiVersion(1, 0))
        .WithApiDefaults()
        .RequireRateLimiting("fixed");


        group.MapGet("/info", (IHostEnvironment env) => Results.Ok(new { env.EnvironmentName, Machine = Environment.MachineName }))
        .AllowAnonymous();


        group.MapGet("/ping", () => Results.Ok("pong"))
        .AllowAnonymous();
    }
}