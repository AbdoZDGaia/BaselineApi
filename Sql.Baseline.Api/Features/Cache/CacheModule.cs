using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.Extensions.Caching.Distributed;
using Sql.Baseline.Api.Infrastructure.Endpoints;


namespace Sql.Baseline.Api.Features.Cache;


public record CacheSetDto(string Key, string Value, int TtlSeconds = 60);


public sealed class CacheModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app, ApiVersionSet versions, IConfiguration cfg)
    {
        var group = app.MapGroup("/api/v{version:apiVersion}/cache")
        .WithTags("Cache")
        .WithApiVersionSet(versions)
        .MapToApiVersion(new ApiVersion(1, 0))
        .WithApiDefaults()
        .RequireRateLimiting("fixed");


        group.MapGet("/{key}", async (string key, IDistributedCache cache) =>
        {
            var value = await cache.GetStringAsync(key);
            return value is null ? Results.NotFound() : Results.Ok(new { key, value });
        }).CacheOutput(p => p.Expire(TimeSpan.FromSeconds(15)));


        group.MapPost("/", async (CacheSetDto dto, IDistributedCache cache) =>
        {
            var opts = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(dto.TtlSeconds)
            };
            await cache.SetStringAsync(dto.Key, dto.Value, opts);
            return Results.Created($"/api/v1/cache/{dto.Key}", new { dto.Key });
        });


        group.MapDelete("/{key}", async (string key, IDistributedCache cache) =>
        {
            await cache.RemoveAsync(key);
            return Results.NoContent();
        });
    }
}