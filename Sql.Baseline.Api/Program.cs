using Serilog;
using Sql.Baseline.Api.Infrastructure.Startup;

var builder = WebApplication.CreateBuilder(args);

// Serilog first
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddBaselineServices(builder.Configuration, builder.Environment);

var app = builder.Build();
Log.Information("Baseline API started at {StartedAt}", DateTimeOffset.UtcNow);
app.UseBaselinePipeline();
app.MapBaselineModules(builder.Configuration);

await app.RunAsync();
