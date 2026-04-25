using System.Data;

using Dapper;

namespace ArchLucid.Persistence.RelationalRead;

/// <summary>Shared COUNT(1) helper for relational slice presence checks (repositories + backfill).</summary>
internal static class SqlRelationalScalarCount
{
    public static async Task<int> ExecuteAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        string sql,
        object param,
        CancellationToken ct)
    {
        int count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, param, transaction,
            cancellationToken: ct));

        return count;
    }
}
