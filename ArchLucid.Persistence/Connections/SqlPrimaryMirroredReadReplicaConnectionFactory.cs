using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Connections;

/// <summary>
///     Routes read-replica-shaped calls to the primary <see cref="ISqlConnectionFactory" /> (CLI, tests, single-connection
///     deployments).
/// </summary>
public sealed class SqlPrimaryMirroredReadReplicaConnectionFactory(ISqlConnectionFactory primary)
    : IGovernanceResolutionReadConnectionFactory, IGoldenManifestLookupReadConnectionFactory
{
    private readonly ISqlConnectionFactory _primary =
        primary ?? throw new ArgumentNullException(nameof(primary));

    /// <inheritdoc />
    public Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct)
    {
        return _primary.CreateOpenConnectionAsync(ct);
    }
}
