using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Tenancy;

namespace ArchLucid.Persistence.Tests.Support;

/// <summary>
///     Builds <see cref="SqlRunRepository" /> with a real <see cref="DapperTenantRepository" /> for SQL integration
///     tests.
/// </summary>
internal static class SqlRunRepositoryTestFactory
{
    public static SqlRunRepository Create(TestSqlConnectionFactory sqlFactory,
        TestAuthorityRunListConnectionFactory listFactory)
    {
        return new SqlRunRepository(sqlFactory, listFactory, new DapperTenantRepository(sqlFactory));
    }
}
