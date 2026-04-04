using ArchiForge.Persistence.Data.Infrastructure;
using ArchiForge.TestSupport;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Tests;

/// <summary>
/// Verifies DbUp can migrate from N−1 (all scripts except the latest) and then apply the tail in a second run (CI upgrade path).
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
        string databaseName = "ArchiForgeMigr_" + suffix;

        SqlConnectionStringBuilder builder = new(fixture.ConnectionString)
        {
            InitialCatalog = databaseName,
        };

        string catalogConnectionString = builder.ConnectionString;

        await SqlServerTestCatalogCommands.EnsureCatalogExistsAsync(catalogConnectionString, CancellationToken.None);

        bool first = DatabaseMigrator.RunExcludingTrailingScripts(catalogConnectionString, 1);
        first.Should().BeTrue("N−1 migration pass should succeed");

        bool second = DatabaseMigrator.Run(catalogConnectionString);
        second.Should().BeTrue("final migration pass should succeed");

        await using SqlConnection connection = new(catalogConnectionString);
        await connection.OpenAsync(CancellationToken.None);

        await using SqlCommand command = new(
            """
            SELECT COL_LENGTH('dbo.PolicyPackAssignments', 'ArchivedUtc') AS Len;
            """,
            connection);

        object? scalar = await command.ExecuteScalarAsync(CancellationToken.None);
        scalar.Should().NotBe(DBNull.Value);
        Convert.ToInt32(scalar, System.Globalization.CultureInfo.InvariantCulture).Should().BeGreaterThan(0);
    }
}
