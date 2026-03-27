using System.Data;

using Microsoft.Data.Sqlite;

namespace ArchiForge.Data.Infrastructure;

/// <summary>
/// SQL Server uses <c>OFFSET … FETCH</c>; SQLite (integration tests) uses <c>LIMIT</c>.
/// </summary>
public static class SqlPagingSyntax
{
    public static bool IsSqlite(IDbConnection connection) => connection is SqliteConnection;

    /// <summary>
    /// Fragment to append after <c>ORDER BY</c>. <paramref name="rowCount"/> must be a trusted constant from code.
    /// </summary>
    public static string FirstRowsOnly(IDbConnection connection, int rowCount)
    {
        if (rowCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowCount));
        }

        if (IsSqlite(connection))
        {
            return $"LIMIT {rowCount}";
        }

        return $"OFFSET 0 ROWS FETCH NEXT {rowCount} ROWS ONLY";
    }
}
