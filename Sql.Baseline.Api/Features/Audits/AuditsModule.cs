using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.EntityFrameworkCore;
using Sql.Baseline.Api.Infrastructure.Data;
using Sql.Baseline.Api.Infrastructure.Data.Auditing;
using Sql.Baseline.Api.Infrastructure.Endpoints;


namespace Sql.Baseline.Api.Features.Audits;


public sealed class AuditsModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app, ApiVersionSet versions, IConfiguration cfg)
    {
        var group = app.MapGroup("/api/v{version:apiVersion}/audits")
        .WithTags("Audits")
        .WithApiVersionSet(versions)
        .MapToApiVersion(new ApiVersion(1, 0))
        .WithApiDefaults()
        .RequireAuthorization("policy:admin")
        .RequireRateLimiting("fixed");


        group.MapGet("/", async (int skip, int take, string? table, string? action, BaselineDbContext db) =>
        {
            var q = db.Set<AuditEntry>().AsNoTracking();
            if (!string.IsNullOrWhiteSpace(table)) q = q.Where(a => a.Table == table);
            if (!string.IsNullOrWhiteSpace(action)) q = q.Where(a => a.Action == action);
            var total = await q.CountAsync();
            var items = await q.OrderByDescending(a => a.CreatedAt).Skip(skip).Take(take).ToListAsync();
            return Results.Ok(new { total, items });
        });
    }
}