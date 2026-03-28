using ArchiForge.Persistence.Advisory;
using ArchiForge.Persistence.Connections;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Runs <see cref="ArchitectureDigestRepositoryContractTests"/> against <see cref="DapperArchitectureDigestRepository"/>
/// backed by a real SQL Server container (<see cref="SqlServerPersistenceFixture"/>).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperArchitectureDigestRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : ArchitectureDigestRepositoryContractTests
{
    protected override IArchitectureDigestRepository CreateRepository()
    {
        return new DapperArchitectureDigestRepository(new SqlConnectionFactory(fixture.ConnectionString));
    }
}
