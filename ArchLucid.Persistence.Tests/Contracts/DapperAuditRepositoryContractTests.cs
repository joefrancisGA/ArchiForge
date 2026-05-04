using ArchLucid.Persistence.Audit;
using ArchLucid.Persistence.Tests.Support;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Runs <see cref="AuditRepositoryContractTests" /> against <see cref="DapperAuditRepository" />.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperAuditRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : AuditRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IAuditRepository CreateRepository()
    {
        return new DapperAuditRepository(new TestSqlConnectionFactory(fixture.ConnectionString));
    }

    protected override async Task EnsureAuditParentRunExistsAsync(
        Guid runId,
        Guid tenantId,
        Guid workspaceId,
        Guid scopeProjectId,
        CancellationToken ct)
    {
        await using SqlConnection connection = new(fixture.ConnectionString);
        await connection.OpenAsync(ct);

        await AuthorityRunChainTestSeed.InsertRunAsync(
            connection,
            tenantId,
            workspaceId,
            scopeProjectId,
            runId,
            scopeProjectId.ToString("D"),
            ct);
    }
}
