using System.Diagnostics.CodeAnalysis;

using Microsoft.Data.SqlClient;

using Microsoft.Extensions.Logging;

namespace ArchiForge.Persistence.Connections;

/// <summary>
/// Decorates <see cref="ResilientSqlConnectionFactory"/> by applying RLS <c>SESSION_CONTEXT</c> after the connection opens.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Decorator over live SQL connection factory; tested via integration tests.")]
public sealed class SessionContextSqlConnectionFactory(
    ResilientSqlConnectionFactory inner,
    IRlsSessionContextApplicator applicator,
    ILogger<SessionContextSqlConnectionFactory> logger) : ISqlConnectionFactory
{
    private readonly ResilientSqlConnectionFactory _inner =
        inner ?? throw new ArgumentNullException(nameof(inner));

    private readonly IRlsSessionContextApplicator _applicator =
        applicator ?? throw new ArgumentNullException(nameof(applicator));

    private readonly ILogger<SessionContextSqlConnectionFactory> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct)
    {
        SqlConnection connection = await _inner.CreateOpenConnectionAsync(ct);

        try
        {
            await _applicator.ApplyAsync(connection, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply SQL session context for RLS.");
            await connection.DisposeAsync();
            throw;
        }

        return connection;
    }
}
