using ArchiForge.Persistence.Provenance;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Runs <see cref="ProvenanceSnapshotRepositoryContractTests"/> against <see cref="SqlProvenanceSnapshotRepository"/>.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperProvenanceSnapshotRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : ProvenanceSnapshotRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IProvenanceSnapshotRepository CreateRepository()
    {
        return new SqlProvenanceSnapshotRepository(new TestSqlConnectionFactory(fixture.ConnectionString));
    }
}
