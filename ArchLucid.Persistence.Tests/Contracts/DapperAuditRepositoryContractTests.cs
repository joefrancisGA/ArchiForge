using ArchLucid.Persistence.Audit;

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
}
