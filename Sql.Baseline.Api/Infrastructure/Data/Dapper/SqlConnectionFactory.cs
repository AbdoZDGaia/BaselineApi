using Microsoft.Data.SqlClient;
using System.Data;

namespace Sql.Baseline.Api.Infrastructure.Data.Dapper;

public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connStr;
    public SqlConnectionFactory(string connStr) => _connStr = connStr;

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
    {
        var conn = new SqlConnection(_connStr);
        await conn.OpenAsync(ct);
        return conn;
    }
}
