using Asp.Versioning.Builder;


namespace Sql.Baseline.Api.Infrastructure.Endpoints;


public interface IEndpointModule
{
    void MapEndpoints(IEndpointRouteBuilder app, ApiVersionSet versions, IConfiguration cfg);
}