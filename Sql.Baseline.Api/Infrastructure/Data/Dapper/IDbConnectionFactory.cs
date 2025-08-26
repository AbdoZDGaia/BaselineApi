using System.Data;

namespace Sql.Baseline.Api.Infrastructure.Data.Dapper;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default);
}
