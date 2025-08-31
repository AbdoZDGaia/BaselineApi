using Npgsql;
using System.Data;

namespace Sql.Baseline.Api.Infrastructure.Data.Dapper;

public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connStr;
    public NpgsqlConnectionFactory(string connStr) => _connStr = connStr;

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
    {
        var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync(ct);
        return conn;
    }
}
