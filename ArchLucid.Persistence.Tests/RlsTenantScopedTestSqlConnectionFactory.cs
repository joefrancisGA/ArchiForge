using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Tests.Support;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     Opens SQL connections where <c>rls.ArchLucidTenantScope</c> predicates see rows for the seeded tenant/workspace/project
///     triple (FK checks on delivery attempts → alert records rely on predicate visibility).
/// </summary>
public sealed class RlsTenantScopedTestSqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;
    private readonly Guid _tenantId;
    private readonly Guid _workspaceId;
    private readonly Guid _projectId;

    public RlsTenantScopedTestSqlConnectionFactory(
        string connectionString,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _tenantId = tenantId;
        _workspaceId = workspaceId;
        _projectId = projectId;
    }

    /// <inheritdoc />
    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct)
    {
        SqlConnection connection = new(_connectionString);
        await connection.OpenAsync(ct);

        await PersistenceIntegrationTestRlsSession.ApplyArchLucidRlsBypassAndTenantScopeAsync(
            connection,
            ct,
            _tenantId,
            _workspaceId,
            _projectId);

        return connection;
    }
}
