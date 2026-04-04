using ArchiForge.Persistence.Data.Repositories;

using ArchiForge.Persistence.Tests.Support;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperArchitectureRunIdempotencyRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : ArchitectureRunIdempotencyRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IArchitectureRunIdempotencyRepository CreateRepository()
    {
        return new ArchitectureRunIdempotencyRepository(new TestSqlDbConnectionFactory(fixture.ConnectionString));
    }

    protected override async Task PrepareRunRowForIdempotencyAsync(string runId, CancellationToken ct)
    {
        string requestId = "idem-req-" + runId;
        await using SqlConnection connection = new(fixture.ConnectionString);
        await connection.OpenAsync(ct);
        await ArchitectureCommitTestSeed.InsertRequestAndRunAsync(connection, requestId, runId, ct);
    }
}
