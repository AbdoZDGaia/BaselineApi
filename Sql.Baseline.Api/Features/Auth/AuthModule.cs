using Asp.Versioning;
using Asp.Versioning.Builder;
using Sql.Baseline.Api.Infrastructure.Endpoints;


namespace Sql.Baseline.Api.Features.Auth;


public sealed class AuthModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app, ApiVersionSet versions, IConfiguration cfg)
    {
        var group = app.MapGroup("/api/v{version:apiVersion}/auth")
        .WithTags("Auth")
        .WithApiVersionSet(versions)
        .MapToApiVersion(new ApiVersion(1, 0))
        .WithApiDefaults()
        .RequireRateLimiting("fixed");


        group.MapGet("/me", (HttpContext ctx) =>
        {
            var user = ctx.User?.Identity?.IsAuthenticated == true ? ctx.User.Identity!.Name : "anonymous";
            var claims = ctx.User?.Claims.Select(c => new { c.Type, c.Value });
            return Results.Ok(new { user, claims });
        });


        group.MapGet("/ping", () => Results.Ok("pong"))
        .AllowAnonymous();
    }
}