using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

namespace Sql.Baseline.Api.Infrastructure.Data.Auditing;

public class EfAuditSaveChangesInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var ctx = (BaselineDbContext)eventData.Context!;
        var audits = new List<AuditEntry>();

        foreach (var entry in ctx.ChangeTracker.Entries<EntityBase>())
        {
            if (entry.State == EntityState.Unchanged) continue;

            var audit = new AuditEntry
            {
                Table = entry.Metadata.GetTableName()!,
                Action = entry.State.ToString(),
                KeyValues = JsonSerializer.Serialize(entry.Properties.Where(p => p.Metadata.IsPrimaryKey()).ToDictionary(p => p.Metadata.Name, p => p.CurrentValue))
            };

            if (entry.State == EntityState.Modified)
            {
                audit.OldValues = JsonSerializer.Serialize(Changed(entry, oldVals: true));
                audit.NewValues = JsonSerializer.Serialize(Changed(entry, oldVals: false));
            }
            else if (entry.State == EntityState.Added)
            {
                audit.NewValues = JsonSerializer.Serialize(entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
            }

            audits.Add(audit);
        }

        if (audits.Count > 0)
            ctx.AddRange(audits);

        return base.SavingChangesAsync(eventData, result, cancellationToken);

        static IDictionary<string, object?> Changed(EntityEntry entry, bool oldVals) =>
            entry.Properties.Where(p => p.IsModified)
                 .ToDictionary(p => p.Metadata.Name, p => oldVals ? p.OriginalValue : p.CurrentValue);
    }
}
