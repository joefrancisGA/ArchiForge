using ArchLucid.Persistence.Data.Infrastructure;
using ArchLucid.TestSupport;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Data.Infrastructure;

/// <summary>
///     Verifies DbUp <see cref="DatabaseMigrator.Run" /> is safe to invoke twice on an already-migrated catalog (no-op
///     journal replay).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlIntegration")]
[Trait("Category", "SqlServerContainer")]
[Trait("Category", "Integration")]
public sealed class DatabaseMigratorSecondRunSqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    [Fact]
    public async Task Second_run_on_same_catalog_does_not_throw()
    {
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable)),
            "Set " + TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable + " to run this SQL integration test.");

        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        string suffix = Guid.NewGuid().ToString("N")[..10];
        string databaseName = "ArchLucidDbUp2x_" + suffix;

        SqlConnectionStringBuilder builder =
            new(fixture.ConnectionString) { InitialCatalog = databaseName };

        string catalogConnectionString = builder.ConnectionString;

        await SqlServerTestCatalogCommands.EnsureCatalogExistsAsync(catalogConnectionString, CancellationToken.None);

        DatabaseMigrator.Run(catalogConnectionString);
        Action second = () => DatabaseMigrator.Run(catalogConnectionString);

        second.Should().NotThrow();
    }
}
