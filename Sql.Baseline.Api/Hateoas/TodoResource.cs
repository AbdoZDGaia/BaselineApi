using Sql.Baseline.Api.Features.Todos;


namespace Sql.Baseline.Api.Hateoas;


public record Link(string Rel, string Href, string Method = "GET");
public interface IResource { List<Link> Links { get; } }


public class TodoResource : IResource
{
    public Guid Id { get; init; }
    public string Title { get; init; } = default!;
    public bool Done { get; init; }
    public List<Link> Links { get; } = new();
}


public static class TodoMapping
{
    public static TodoResource ToResource(this Todo e, LinkGenerator lg, HttpContext ctx)
    {
        var href = $"/api/v1/todos/{e.Id}";
        return new TodoResource
        {
            Id = e.Id,
            Title = e.Title,
            Done = e.Done
        }
        .WithLink(new Link("self", href))
        .WithLink(new Link("mark-done", $"{href}/done", "POST"))
        .WithLink(new Link("delete", href, "DELETE"));
    }


    public static T WithLink<T>(this T resource, Link link) where T : IResource
    { resource.Links.Add(link); return resource; }
}