using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Connections;

/// <summary>
///     Detects SQL Server unique-key violations (e.g. concurrent duplicate inserts) for application-level reconciliation.
/// </summary>
/// <remarks>
///     <see cref="SqlException.Number" /> <c>2601</c> / <c>2627</c>: duplicate key / unique constraint (including parallel
///     first-commit racing on the same manifest version).
/// </remarks>
public static class SqlUniqueConstraintViolationDetector
{
    /// <summary>
    ///     Returns <see langword="true" /> when <paramref name="ex" /> or an inner exception is a unique-key
    ///     <see cref="SqlException" />.
    /// </summary>
    public static bool IsUniqueKeyViolation(Exception? ex)
    {
        if (ex is null)
            return false;

        return ex is SqlException { Number: 2601 or 2627 } || IsUniqueKeyViolation(ex.InnerException);
    }
}
