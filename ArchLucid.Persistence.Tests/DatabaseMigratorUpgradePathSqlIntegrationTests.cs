using System.Globalization;

using ArchLucid.Persistence.Data.Infrastructure;
using ArchLucid.TestSupport;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     Verifies DbUp can migrate from Nâˆ’1 (all scripts except the latest) and then apply the tail in a second run (CI
///     upgrade path).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DatabaseMigratorUpgradePathSqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    [SkippableFact]
    public async Task RunExcludingLatest_then_full_run_succeeds()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        string suffix = Guid.NewGuid().ToString("N")[..10];
        string databaseName = "ArchLucidMigr_" + suffix;

        SqlConnectionStringBuilder builder = new(fixture.ConnectionString) { InitialCatalog = databaseName };

        string catalogConnectionString = builder.ConnectionString;

        await SqlServerTestCatalogCommands.EnsureCatalogExistsAsync(catalogConnectionString, CancellationToken.None);

        DatabaseMigrator.RunExcludingTrailingScripts(catalogConnectionString, 1);
        DatabaseMigrator.Run(catalogConnectionString);

        await using SqlConnection connection = new(catalogConnectionString);
        await connection.OpenAsync(CancellationToken.None);

        await using SqlCommand command = new(
            """
            SELECT COL_LENGTH('dbo.PolicyPackAssignments', 'ArchivedUtc') AS Len;
            """,
            connection);

        object? scalar = await command.ExecuteScalarAsync(CancellationToken.None);
        scalar.Should().NotBe(DBNull.Value);
        Convert.ToInt32(scalar, CultureInfo.InvariantCulture).Should().BeGreaterThan(0);
    }
}
