using Microsoft.EntityFrameworkCore;
using Sql.Baseline.Api.Features.Todos;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Linq;

namespace Sql.Baseline.Api.Infrastructure.Data;

public class BaselineDbContext : DbContext
{
    private readonly IHttpContextAccessor _http;

    public BaselineDbContext(DbContextOptions<BaselineDbContext> options, IHttpContextAccessor http) : base(options)
    {
        _http = http;
    }

    public DbSet<Todo> Todos => Set<Todo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var (entityType, filter) in from entityType in modelBuilder.Model.GetEntityTypes()
                                             where typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)
                                             let param = Expression.Parameter(entityType.ClrType, "e")
                                             let prop = Expression.Property(param, nameof(ISoftDeletable.IsDeleted))
                                             let body = Expression.Equal(prop, Expression.Constant(false))
                                             let filter = Expression.Lambda(body, param)
                                             select (entityType, filter))
        {
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var user = _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var now = DateTimeOffset.UtcNow;

        foreach (var e in ChangeTracker.Entries().ToList())
        {
            if (e.Entity is EntityBase eb)
            {
                switch (e.State)
                {
                    case EntityState.Added:
                        eb.CreatedAt = now; eb.CreatedBy = user; break;
                    case EntityState.Modified:
                        eb.UpdatedAt = now; eb.UpdatedBy = user; break;
                    case EntityState.Deleted when e.Entity is ISoftDeletable sd:
                        e.State = EntityState.Modified;
                        sd.IsDeleted = true; sd.DeletedAt = now; sd.DeletedBy = user;
                        break;
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
