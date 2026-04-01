using ArchiForge.Data.Repositories;

using ArchiForge.Persistence.Tests.Support;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperAgentEvidencePackageRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : AgentEvidencePackageRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IAgentEvidencePackageRepository CreateRepository()
    {
        return new AgentEvidencePackageRepository(new TestSqlDbConnectionFactory(fixture.ConnectionString));
    }

    protected override async Task PrepareRequestAndRunAsync(string requestId, string runId, CancellationToken ct)
    {
        await using SqlConnection connection = new(fixture.ConnectionString);
        await connection.OpenAsync(ct);
        await ArchitectureCommitTestSeed.InsertRequestAndRunAsync(connection, requestId, runId, ct);
    }
}
