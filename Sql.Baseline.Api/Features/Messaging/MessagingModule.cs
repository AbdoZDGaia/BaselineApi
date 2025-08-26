using Asp.Versioning;
using Asp.Versioning.Builder;
using Sql.Baseline.Api.Infrastructure.Email;
using Sql.Baseline.Api.Infrastructure.Endpoints;
using Sql.Baseline.Api.Infrastructure.Sms;


namespace Sql.Baseline.Api.Features.Messaging;


public record SendEmailDto(string To, string Subject, string HtmlBody);
public record SendSmsDto(string To, string Body);


public sealed class MessagingModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app, ApiVersionSet versions, IConfiguration cfg)
    {
        var group = app.MapGroup("/api/v{version:apiVersion}/messaging")
        .WithTags("Messaging")
        .WithApiVersionSet(versions)
        .MapToApiVersion(new ApiVersion(1, 0))
        .WithApiDefaults()
        .RequireAuthorization("can:notify")
        .RequireRateLimiting("burst");


        group.MapPost("/email", async (SendEmailDto dto, ISmtpEmailSender email) =>
        {
            await email.SendAsync(dto.To, dto.Subject, dto.HtmlBody);
            return Results.Accepted();
        });


        group.MapPost("/sms", async (SendSmsDto dto, ITwilioSmsSender sms) =>
        {
            await sms.SendAsync(dto.To, dto.Body);
            return Results.Accepted();
        });
    }
}