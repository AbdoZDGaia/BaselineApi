namespace Sql.Baseline.Api.Infrastructure.Data.Auditing;

public class AuditEntry : EntityBase
{
    public string Table { get; set; } = default!;
    public string Action { get; set; } = default!; // Insert/Update/Delete
    public string KeyValues { get; set; } = default!;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}
