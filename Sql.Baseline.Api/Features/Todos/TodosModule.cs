using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.EntityFrameworkCore;
using Sql.Baseline.Api.Hateoas;
using Sql.Baseline.Api.Infrastructure.Data;
using Sql.Baseline.Api.Infrastructure.Endpoints;


namespace Sql.Baseline.Api.Features.Todos;


public sealed class TodosModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app, ApiVersionSet versions, IConfiguration cfg)
    {
        var group = app.MapGroup("/api/v{version:apiVersion}/todos")
        .WithTags("Todos")
        .WithApiVersionSet(versions)
        .MapToApiVersion(new ApiVersion(1, 0))
        .WithApiDefaults()
        .RequireRateLimiting("fixed")
        .CacheOutput(p => p.Expire(TimeSpan.FromSeconds(20)));


        group.MapGet("/", async (BaselineDbContext db, LinkGenerator lg, HttpContext ctx) =>
        {
            var res = await db.Todos.AsNoTracking().Take(100).ToListAsync();
            return Results.Ok(res.Select(t => t.ToResource(lg, ctx)));
        });


        group.MapGet("/{id:guid}", async (Guid id, BaselineDbContext db, LinkGenerator lg, HttpContext ctx) =>
        {
            var e = await db.Todos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return e is null ? Results.NotFound() : Results.Ok(e.ToResource(lg, ctx));
        });


        group.MapPost("/", async (CreateTodo body, BaselineDbContext db, LinkGenerator lg, HttpContext ctx) =>
        {
            var todo = new Todo { Title = body.Title };
            db.Add(todo);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/todos/{todo.Id}", todo.ToResource(lg, ctx));
        });


        group.MapPost("/{id:guid}/done", async (Guid id, BaselineDbContext db) =>
        {
            var e = await db.Todos.FindAsync(id);
            if (e is null) return Results.NotFound();
            e.Done = true;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });


        group.MapDelete("/{id:guid}", async (Guid id, BaselineDbContext db) =>
        {
            var e = await db.Todos.FindAsync(id);
            if (e is null) return Results.NotFound();
            db.Remove(e); // soft-delete
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}