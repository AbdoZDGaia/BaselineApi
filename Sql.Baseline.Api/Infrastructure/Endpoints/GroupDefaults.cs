namespace Sql.Baseline.Api.Infrastructure.Endpoints;


public static class GroupDefaults
{
    public static RouteGroupBuilder WithApiDefaults(this RouteGroupBuilder group)
    => group.RequireAuthorization()
    .AddEndpointFilter<ValidationFilter>();
}