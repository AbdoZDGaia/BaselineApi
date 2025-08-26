using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.SignalR;
using Sql.Baseline.Api.Infrastructure.Endpoints;
using Sql.Baseline.Api.Infrastructure.Realtime;


namespace Sql.Baseline.Api.Features.Notifications;


public record BroadcastDto(string Event, object Payload);


public sealed class NotificationsModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app, ApiVersionSet versions, IConfiguration cfg)
    {
        // Hub mapping is part of the module (keeps it encapsulated)
        app.MapHub<NotificationsHub>("/realtime");


        var group = app.MapGroup("/api/v{version:apiVersion}/notifications")
        .WithTags("Notifications")
        .WithApiVersionSet(versions)
        .MapToApiVersion(new ApiVersion(1, 0))
        .WithApiDefaults()
        .RequireAuthorization("can:notify")
        .RequireRateLimiting("burst");


        group.MapPost("/broadcast", async (BroadcastDto dto, IHubContext<NotificationsHub> hub) =>
        {
            await hub.Clients.All.SendAsync(dto.Event, dto.Payload);
            return Results.Accepted();
        });
    }
}