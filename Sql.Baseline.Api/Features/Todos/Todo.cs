using Sql.Baseline.Api.Infrastructure.Data;


namespace Sql.Baseline.Api.Features.Todos;


public class Todo : EntityBase, ISoftDeletable, IAuditable
{
    public string Title { get; set; } = default!;
    public bool Done { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}


public record CreateTodo(string Title);
public record UpdateTodo(string Title, bool Done);