using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace ArchLucid.Persistence.Connections;

/// <summary>
///     Opens either a read-scale-out connection string resolved for <paramref name="route" /> or the primary
///     <see cref="ResilientSqlConnectionFactory" /> path, then applies <see cref="IRlsSessionContextApplicator" />.
/// </summary>
public sealed class ReadReplicaRoutedConnectionFactory(
    ResilientSqlConnectionFactory resilientFactory,
    IOptionsMonitor<SqlServerOptions> optionsMonitor,
    IRlsSessionContextApplicator sessionContextApplicator,
    ReadReplicaQueryRoute route) : IAuthorityRunListConnectionFactory, IGovernanceResolutionReadConnectionFactory,
    IGoldenManifestLookupReadConnectionFactory
{
    private readonly IOptionsMonitor<SqlServerOptions> _optionsMonitor =
        optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

    private readonly ResilientSqlConnectionFactory _resilientFactory =
        resilientFactory ?? throw new ArgumentNullException(nameof(resilientFactory));

    private readonly IRlsSessionContextApplicator _sessionContextApplicator =
        sessionContextApplicator ?? throw new ArgumentNullException(nameof(sessionContextApplicator));

    /// <inheritdoc />
    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct)
    {
        SqlServerOptions snapshot = _optionsMonitor.CurrentValue;
        string? replica = SqlReadReplicaConnectionStringResolver.Resolve(route, snapshot.ReadReplica);

        SqlConnection connection;
        if (string.IsNullOrEmpty(replica))
            connection = await _resilientFactory.CreateOpenConnectionAsync(ct);
        else
        {
            connection = new SqlConnection(replica);
            await connection.OpenAsync(ct);
        }

        await _sessionContextApplicator.ApplyAsync(connection, ct);
        return connection;
    }
}
