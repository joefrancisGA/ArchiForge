using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Repositories;
using ArchiForge.Persistence.Tests.Support;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Runs <see cref="GoldenManifestRepositoryContractTests"/> against <see cref="SqlGoldenManifestRepository"/>.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class SqlGoldenManifestRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : GoldenManifestRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IGoldenManifestRepository CreateRepository()
    {
        return new SqlGoldenManifestRepository(new TestSqlConnectionFactory(fixture.ConnectionString));
    }

    protected override async Task PrepareAuthorityChainForManifestAsync(
        ScopeContext scope,
        Guid runId,
        Guid contextId,
        Guid graphId,
        Guid findingsId,
        Guid traceId,
        CancellationToken ct)
    {
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(ct);
        await AuthorityRunChainTestSeed.SeedFullChainAsync(
            connection,
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            runId,
            contextId,
            graphId,
            findingsId,
            traceId,
            "contract-proj-gm",
            ct);
    }
}
