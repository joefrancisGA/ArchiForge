using ArchLucid.Persistence.Data.Infrastructure;
using ArchLucid.TestSupport;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Data.Infrastructure;

/// <summary>
///     Verifies <see cref="GreenfieldBaselineMigrationRunner.TryApplyBaselineAndStampThrough050" /> plus DbUp recover when
///     <c>dbo.SchemaVersions</c> is emptied while physical schema already reflects the latest migrations (journal drift).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class JournalDriftBaselineRepairSqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    [SkippableFact]
    public async Task Empty_SchemaVersions_then_DatabaseMigrator_Run_repairs_journal_without_duplicate_object_errors()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        string suffix = Guid.NewGuid().ToString("N")[..10];
        string databaseName = "ArchLucidJournalDrift_" + suffix;

        SqlConnectionStringBuilder builder =
            new(fixture.ConnectionString) { InitialCatalog = databaseName };

        string catalogConnectionString =
            SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(builder.ConnectionString);

        await SqlServerTestCatalogCommands.EnsureCatalogExistsAsync(catalogConnectionString, CancellationToken.None);

        DatabaseMigrator.Run(catalogConnectionString);

        await using (SqlConnection connection = new(catalogConnectionString))
        {
            await connection.OpenAsync(CancellationToken.None);

            await using SqlCommand truncate = new("DELETE FROM dbo.SchemaVersions;", connection);

            int deleted = await truncate.ExecuteNonQueryAsync(CancellationToken.None);

            deleted.Should().BeGreaterThan(0);
        }

        Action repair = () => DatabaseMigrator.Run(catalogConnectionString);

        repair.Should().NotThrow();

        await using SqlConnection verify = new(catalogConnectionString);
        await verify.OpenAsync(CancellationToken.None);

        await using SqlCommand count = new("SELECT COUNT(*) FROM dbo.SchemaVersions;", verify);

        object? rows = await count.ExecuteScalarAsync(CancellationToken.None);

        Convert.ToInt32(rows, System.Globalization.CultureInfo.InvariantCulture).Should().BeGreaterThan(0);
    }
}
