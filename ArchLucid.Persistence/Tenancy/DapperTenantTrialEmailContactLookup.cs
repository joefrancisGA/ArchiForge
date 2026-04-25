using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tenancy;

/// <summary>Uses the latest <c>TrialProvisioned</c> or <c>TenantSelfRegistered</c> audit actor id as the mailbox.</summary>
public sealed class DapperTenantTrialEmailContactLookup(ISqlConnectionFactory connectionFactory)
    : ITenantTrialEmailContactLookup
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    /// <inheritdoc />
    public async Task<string?> TryResolveAdminEmailAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
                           SELECT TOP (1) ActorUserId
                           FROM dbo.AuditEvents
                           WHERE TenantId = @TenantId
                             AND EventType IN (N'TrialProvisioned', N'TenantSelfRegistered')
                           ORDER BY OccurredUtc DESC;
                           """;

        string? actor = await connection.ExecuteScalarAsync<string?>(
            new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken));

        if (string.IsNullOrWhiteSpace(actor))
            return null;


        string trimmed = actor.Trim();

        return trimmed.Contains('@', StringComparison.Ordinal) ? trimmed : null;
    }
}
