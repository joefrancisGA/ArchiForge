using ArchLucid.Persistence.Data.Infrastructure;
using ArchLucid.Persistence.RelationalRead;
using ArchLucid.TestSupport;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Data.Infrastructure;

/// <summary>
///     Exercises <see cref="SqlRelationalScalarCount" /> against SQL Server (covers Dapper scalar path used by
///     repositories and backfill helpers).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlIntegration")]
[Trait("Category", "SqlServerContainer")]
[Trait("Category", "Integration")]
public sealed class SqlRelationalScalarCountSqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    [SkippableFact]
    public async Task ExecuteAsync_returns_count_for_simple_aggregate()
    {
        Skip.If(
            string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable)),
            "Set " + TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable + " to run this SQL integration test.");

        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        TestSqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(CancellationToken.None);

        int count = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            null,
            "SELECT COUNT(1) FROM sys.tables WHERE name = N'Runs';",
            new { },
            CancellationToken.None);

        count.Should().Be(1);
    }
}
