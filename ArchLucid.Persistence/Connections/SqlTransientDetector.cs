using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Connections;

/// <summary>
/// Identifies transient SQL Server error codes that are safe to retry.
/// Shared by <c>SqlConnectionHealthCheck</c> and <c>ResilientSqlConnectionFactory</c>.
/// </summary>
/// <remarks>
/// Error numbers: <c>-2</c> = timeout; <c>40613</c> = Azure SQL DB unavailable;
/// <c>40197</c> = service error during processing; <c>49918–49920</c> = Azure throttling.
/// </remarks>
public static class SqlTransientDetector
{
    /// <summary>Returns <see langword="true"/> when the <see cref="SqlException"/> represents a transient, retriable condition.</summary>
    public static bool IsTransient(SqlException? ex)
    {
        if (ex is null)
            return false;

        return ex.Number is -2 or 40613 or 40197 or 49918 or 49919 or 49920;
    }

    /// <summary>Returns <see langword="true"/> when the exception (or its inner exception) is a transient SQL or timeout error.</summary>
    public static bool IsTransient(Exception? ex)
    {
        if (ex is null)
            return false;

        if (ex is SqlException sqlEx)
            return IsTransient(sqlEx);

        if (ex is TimeoutException)
            return true;

        if (ex.InnerException is SqlException innerSql)
            return IsTransient(innerSql);

        return false;
    }
}
