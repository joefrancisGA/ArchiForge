using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Connections;

/// <summary>
///     Identifies transient SQL Server error codes that are safe to retry.
///     Shared by <c>SqlConnectionHealthCheck</c> and <c>ResilientSqlConnectionFactory</c>.
/// </summary>
/// <remarks>
///     Error numbers: <c>-2</c> = timeout; <c>1205</c> = deadlock victim (safe to retry when the UoW rolled back);
///     <c>40613</c> = Azure SQL DB unavailable; <c>40197</c> = service error during processing; <c>49918–49920</c> = Azure
///     throttling.
/// </remarks>
public static class SqlTransientDetector
{
    /// <summary>
    ///     Returns <see langword="true" /> when the <see cref="SqlException" /> represents a transient, retriable
    ///     condition.
    /// </summary>
    public static bool IsTransient(SqlException? ex)
    {
        if (ex is null)
            return false;

        return ex.Number is -2 or 1205 or 40613 or 40197 or 49918 or 49919 or 49920;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the exception or any nested <see cref="Exception.InnerException" /> is a
    ///     transient SQL or timeout error.
    /// </summary>
    /// <remarks>
    ///     Dapper and repository layers may wrap <see cref="SqlException" /> more than one level deep; parallel commits
    ///     can surface deadlock (<c>1205</c>) inside wrappers — walking the full chain keeps commit retries effective.
    /// </remarks>
    public static bool IsTransient(Exception? ex)
    {
        if (ex is null)
            return false;

        for (Exception? e = ex; e is not null; e = e.InnerException)
        {
            if (e is TimeoutException)
                return true;

            if (e is SqlException sqlEx && IsTransient(sqlEx))
                return true;
        }

        return false;
    }
}
