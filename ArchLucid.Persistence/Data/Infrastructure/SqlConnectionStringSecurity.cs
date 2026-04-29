using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Data.Infrastructure;

/// <summary>
///     Normalizes SQL client connection strings for in-transit encryption (CWE-311 / CodeQL
///     <c>cs/insecure-sql-connection</c>).
/// </summary>
/// <remarks>
///     <see cref="SqlConnectionStringBuilder" /> with <see cref="SqlConnectionEncryptOption.Mandatory" /> matches
///     <c>Encrypt=True</c> / mandatory TLS in modern <see cref="Microsoft.Data.SqlClient" /> and satisfies static analysis
///     that looks for an explicit client-side encryption setting. <c>TrustServerCertificate</c> (e.g. local dev) is unchanged
///     when already present.
/// </remarks>
public static class SqlConnectionStringSecurity
{
    /// <summary>
    ///     Returns a connection string that requires encrypted transport to SQL Server.
    /// </summary>
    public static string EnsureSqlClientEncryptMandatory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required.", nameof(connectionString));

        SqlConnectionStringBuilder builder = new(connectionString.Trim())
        {
            Encrypt = SqlConnectionEncryptOption.Mandatory
        };

        return builder.ConnectionString;
    }
}
