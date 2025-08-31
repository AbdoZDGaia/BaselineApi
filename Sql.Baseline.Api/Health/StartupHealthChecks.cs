using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sql.Baseline.Api.Health;

public static class StartupHealthChecks
{
    public static IServiceCollection AddAppHealthChecks(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddHealthChecks()
            .AddNpgSql(cfg.GetConnectionString("Default")!)
            .AddRedis(cfg["Redis:ConnectionString"]!);
        return services;
    }
}
