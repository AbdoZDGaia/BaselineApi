using Asp.Versioning;
using Dapper;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Sql.Baseline.Api.Features.Audits;
using Sql.Baseline.Api.Features.Auth;
using Sql.Baseline.Api.Features.Cache;
using Sql.Baseline.Api.Features.Diagnostics;
using Sql.Baseline.Api.Features.Messaging;
using Sql.Baseline.Api.Features.Notifications;
using Sql.Baseline.Api.Features.Reports;
using Sql.Baseline.Api.Features.Todos;
using Sql.Baseline.Api.Health;
using Sql.Baseline.Api.Infrastructure.Data;
using Sql.Baseline.Api.Infrastructure.Data.Auditing;
using Sql.Baseline.Api.Infrastructure.Data.Dapper;
using Sql.Baseline.Api.Infrastructure.Endpoints;
using Sql.Baseline.Api.Middleware;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Serilog first
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Host.UseSerilog();

// Db + EF Core
builder.Services.AddSingleton<SaveChangesInterceptor, EfAuditSaveChangesInterceptor>();

builder.Services.AddDbContext<BaselineDbContext>((sp, opt) =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default"))
       .AddInterceptors(sp.GetRequiredService<SaveChangesInterceptor>());
});

builder.Services.AddScoped<IDbConnectionFactory>(_ =>
    new SqlConnectionFactory(builder.Configuration.GetConnectionString("Default")!));

// Dapper default settings
DefaultTypeMap.MatchNamesWithUnderscores = true;

// AuthZ/AuthN (JWT)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Jwt:Authority"];
        options.Audience = builder.Configuration["Jwt:Audience"];
        options.RequireHttpsMetadata = bool.Parse(builder.Configuration["Jwt:RequireHttpsMetadata"] ?? "true");
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// API versioning
builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;
}).AddApiExplorer();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Baseline API", Version = "v1" });
    var jwtScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter JWT Bearer token"
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new()
    {
        [jwtScheme] = Array.Empty<string>()
    });
});

// Validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Output caching + Rate limiting
builder.Services.AddOutputCache();
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(10);
        opt.PermitLimit = 100;
        opt.QueueLimit = 0;
    });
});

// Redis cache + Data Protection
builder.Services.AddStackExchangeRedisCache(o =>
    o.Configuration = builder.Configuration["Redis:ConnectionString"]);

// SignalR
builder.Services.AddSignalR();

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Sql.Baseline.Api"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation(o => { o.SetDbStatementForText = true; })
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

// Health checks
builder.Services.AddAppHealthChecks(builder.Configuration);

// CORS (adjust as needed)
builder.Services.AddCors(o =>
    o.AddPolicy("default", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();


// Pipeline (keep as before):
app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingEnricherMiddleware>();


if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }


app.UseExceptionHandler("/error");
app.UseHttpsRedirection();
app.UseCors("default");
app.UseRateLimiter();
app.UseOutputCache();
app.UseAuthentication();
app.UseAuthorization();


// Health endpoints (global)
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");


// Version set (one place)
var versionSet = app.NewApiVersionSet("Baseline API")
.HasApiVersion(new ApiVersion(1, 0))
.ReportApiVersions()
.Build();


// Feature toggles
bool Enabled(string key) => builder.Configuration.GetValue<bool>($"Features:{key}");


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
    m.MapEndpoints(app, versionSet, builder.Configuration);


await app.RunAsync();
