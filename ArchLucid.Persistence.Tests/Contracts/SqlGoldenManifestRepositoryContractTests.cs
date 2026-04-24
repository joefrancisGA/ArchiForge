using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Tests.Support;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Runs <see cref="GoldenManifestRepositoryContractTests" /> against <see cref="SqlGoldenManifestRepository" />.
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
        return SqlPersistenceRepositoryFactory.CreateGoldenManifestRepository(
            new TestSqlConnectionFactory(fixture.ConnectionString));
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
