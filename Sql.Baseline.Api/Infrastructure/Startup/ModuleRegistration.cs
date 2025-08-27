using Asp.Versioning;
using Sql.Baseline.Api.Features.Audits;
using Sql.Baseline.Api.Features.Auth;
using Sql.Baseline.Api.Features.Cache;
using Sql.Baseline.Api.Features.Diagnostics;
using Sql.Baseline.Api.Features.Messaging;
using Sql.Baseline.Api.Features.Notifications;
using Sql.Baseline.Api.Features.Reports;
using Sql.Baseline.Api.Features.Todos;
using Sql.Baseline.Api.Infrastructure.Endpoints;

namespace Sql.Baseline.Api.Infrastructure.Startup;

public static class ModuleRegistration
{
    public static void MapBaselineModules(this WebApplication app, IConfiguration cfg)
    {
        var versionSet = app.NewApiVersionSet("Baseline API")
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        bool Enabled(string key) => cfg.GetValue<bool>($"Features:{key}");

        var modules = new List<IEndpointModule>();
        if (Enabled("Todos")) modules.Add(new TodosModule());
        if (Enabled("Messaging")) modules.Add(new MessagingModule());
        if (Enabled("Notifications")) modules.Add(new NotificationsModule());
        if (Enabled("Audits")) modules.Add(new AuditsModule());
        if (Enabled("Cache")) modules.Add(new CacheModule());
        if (Enabled("Reports")) modules.Add(new ReportsModule());
        if (Enabled("Auth")) modules.Add(new AuthModule());
        if (Enabled("Diagnostics")) modules.Add(new DiagnosticsModule());

        foreach (var m in modules)
            m.MapEndpoints(app, versionSet, cfg);
    }
}
