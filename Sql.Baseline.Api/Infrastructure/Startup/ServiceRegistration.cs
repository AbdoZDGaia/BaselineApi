using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sql.Baseline.Api.Health;
using Sql.Baseline.Api.Infrastructure.Data;
using Sql.Baseline.Api.Infrastructure.Data.Auditing;
using Sql.Baseline.Api.Infrastructure.Data.Dapper;
using Sql.Baseline.Api.Infrastructure.Email;
using Sql.Baseline.Api.Infrastructure.Sms;
using System.Reflection;

namespace Sql.Baseline.Api.Infrastructure.Startup;

public static class ServiceRegistration
{
    public static IServiceCollection AddBaselineServices(this IServiceCollection services, IConfiguration cfg, IHostEnvironment env)
    {
        // Serilog is configured on Host; nothing here.

        services.AddHttpContextAccessor();

        // EF + Interceptor
        services.AddSingleton<SaveChangesInterceptor, EfAuditSaveChangesInterceptor>();
        services.AddDbContext<BaselineDbContext>((sp, opt) =>
        {
            opt.UseSqlServer(cfg.GetConnectionString("Default"))
               .AddInterceptors(sp.GetRequiredService<SaveChangesInterceptor>());
        });

        // Dapper connection factory
        services.AddScoped<IDbConnectionFactory>(_ => new SqlConnectionFactory(cfg.GetConnectionString("Default")!));

        // AuthN/Z
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.Authority = cfg["Jwt:Authority"];
                o.Audience = cfg["Jwt:Audience"];
                o.RequireHttpsMetadata = bool.Parse(cfg["Jwt:RequireHttpsMetadata"] ?? "true");
            });
        services.AddAuthorization(options =>
        {
            options.AddPolicy("policy:admin", p =>
                p.RequireAuthenticatedUser().RequireClaim("role", "admin"));

            options.AddPolicy("policy:report", p =>
                p.RequireAuthenticatedUser().RequireClaim("role", "report", "admin"));

            options.AddPolicy("can:notify", p =>
                p.RequireAuthenticatedUser().RequireClaim("permissions", "notify:send"));
        });

        // Versioning + Swagger
        services.AddApiVersioning(o =>
        {
            o.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
            o.AssumeDefaultVersionWhenUnspecified = true;
            o.ReportApiVersions = true;
        })
        .AddApiExplorer(o =>
        {
            // makes groups like "v1"
            o.GroupNameFormat = "'v'VVV";
            // replaces the {version:apiVersion} route token with the actual version in the Swagger doc
            o.SubstituteApiVersionInUrl = true;
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "Baseline API", Version = "v1" });
            var jwt = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "Enter JWT Bearer token"
            };
            c.AddSecurityDefinition("Bearer", jwt);
            c.AddSecurityRequirement(new() { [jwt] = Array.Empty<string>() });
        });

        // Validation
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Caching + Rate Limiting
        services.AddOutputCache();
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("fixed", opt =>
            {
                opt.Window = TimeSpan.FromSeconds(10);
                opt.PermitLimit = 100;
                opt.QueueLimit = 0;
            });
        });

        // Redis
        services.AddStackExchangeRedisCache(o => o.Configuration = cfg["Redis:ConnectionString"]);

        // SignalR
        services.AddSignalR();

        // OpenTelemetry
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("Sql.Baseline.Api"))
            .WithTracing(t => t
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation(o => o.SetDbStatementForText = true)
                .AddOtlpExporter())
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter());

        // Health checks
        services.AddAppHealthChecks(cfg);

        // CORS
        services.AddCors(o => o.AddPolicy("default", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

        // Filters
        services.AddScoped<ValidationFilter>();

        services.AddTransient<ISmtpEmailSender, SmtpEmailSender>();
        services.AddTransient<ITwilioSmsSender, TwilioSmsSender>();

        return services;
    }
}