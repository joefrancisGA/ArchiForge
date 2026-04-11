using ArchLucid.Persistence.Data.Repositories;

using ArchLucid.Persistence.Tests.Support;

using Microsoft.Data.SqlClient;

#pragma warning disable CS0618 // RunsAuthorityConvergence: tracked for migration by 2026-09-30 — contract tests for legacy ArchitectureRunRepository.

namespace ArchLucid.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperArchitectureRunRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : ArchitectureRunRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IArchitectureRunRepository CreateRepository()
    {
        return new ArchitectureRunRepository(new TestSqlDbConnectionFactory(fixture.ConnectionString));
    }

    protected override async Task PrepareRequestRowAsync(string requestId, string systemName, CancellationToken ct)
    {
        await using SqlConnection connection = new(fixture.ConnectionString);
        await connection.OpenAsync(ct);
        await ArchitectureCommitTestSeed.InsertArchitectureRequestOnlyAsync(connection, requestId, systemName, ct);
    }
}

#pragma warning restore CS0618
