using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tenancy.Diagnostics;

public sealed class DapperTrialFunnelOperationalMetricsReader(ISqlConnectionFactory connectionFactory)
    : ITrialFunnelOperationalMetricsReader
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    public async Task<long> CountActiveSelfServiceTrialsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT_BIG(1)
            FROM dbo.Tenants
            WHERE TrialExpiresUtc IS NOT NULL
              AND TrialStatus = @Active;
            """;

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        long count = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                sql,
                new { Active = TrialLifecycleStatus.Active },
                cancellationToken: cancellationToken));

        return count;
    }
}
