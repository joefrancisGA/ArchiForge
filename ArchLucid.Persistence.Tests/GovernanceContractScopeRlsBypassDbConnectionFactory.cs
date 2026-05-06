using System.Data;

using ArchLucid.Persistence.Data.Infrastructure;
using ArchLucid.Persistence.Tests.Support;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     Connection factory for governance SQL contract tests: applies RLS bypass plus the fixed
///     <see cref="GovernanceRepositoryContractScope" /> triple on every opened connection.
///     Aligns read paths with <see cref="SqlServerPersistenceFixture.MergeGovernanceContractTenantAsync" /> so rows remain
///     visible when pooled sessions drop <c>al_rls_bypass</c> while FILTER predicates still evaluate tenant keys.
/// </summary>
public sealed class GovernanceContractScopeRlsBypassDbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public GovernanceContractScopeRlsBypassDbConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(connectionString.Trim());
    }

    /// <inheritdoc />
    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    /// <inheritdoc />
    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        SqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await PersistenceIntegrationTestRlsSession.ApplyArchLucidRlsBypassAndTenantScopeAsync(
            connection,
            cancellationToken,
            GovernanceRepositoryContractScope.TenantId,
            GovernanceRepositoryContractScope.WorkspaceId,
            GovernanceRepositoryContractScope.ProjectId,
            ambientTransaction: null);

        return connection;
    }
}
