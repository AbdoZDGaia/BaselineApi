using Asp.Versioning;
using Asp.Versioning.Builder;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Sql.Baseline.Api.Infrastructure.Data;
using Sql.Baseline.Api.Infrastructure.Endpoints;


namespace Sql.Baseline.Api.Features.Reports;


public sealed class ReportsModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app, ApiVersionSet versions, IConfiguration cfg)
    {
        var group = app.MapGroup("/api/v{version:apiVersion}/reports")
        .WithTags("Reports")
        .WithApiVersionSet(versions)
        .MapToApiVersion(new ApiVersion(1, 0))
        .WithApiDefaults()
        .RequireAuthorization("policy:report")
        .RequireRateLimiting("fixed");


        // Example: top N items via Dapper (high-perf read)
        group.MapGet("/top-todos", async (int take, BaselineDbContext db) =>
        {
            var conn = db.Database.GetDbConnection();
            var rows = await conn.QueryAsync<(Guid Id, string Title, bool Done)>(
            "SELECT TOP(@take) Id, Title, Done FROM Todos WHERE IsDeleted = 0 ORDER BY CreatedAt DESC",
            new { take }, transaction: db.Database.CurrentTransaction?.GetDbTransaction());
            return Results.Ok(rows);
        }).CacheOutput(p => p.Expire(TimeSpan.FromSeconds(30)));
    }
}