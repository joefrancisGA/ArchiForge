using ArchLucid.Persistence.Data.Infrastructure;
using ArchLucid.TestSupport;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Data.Infrastructure;

/// <summary>
///     Verifies hand-maintained rollback scripts under <c>Migrations/Rollback/</c> execute cleanly against a catalog that
///     has applied the matching forward migration (manual rollback path â€” not invoked by DbUp).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class MigrationRollbackScriptsSqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    [SkippableFact]
    public async Task R124_FindingRecords_FilterIndexes_drops_expected_indexes_after_forward_migration()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        string suffix = Guid.NewGuid().ToString("N")[..10];
        string databaseName = "ArchLucidRb124_" + suffix;

        SqlConnectionStringBuilder builder =
            new(fixture.ConnectionString) { InitialCatalog = databaseName };

        string catalogConnectionString =
            SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(builder.ConnectionString);

        await SqlServerTestCatalogCommands.EnsureCatalogExistsAsync(catalogConnectionString, CancellationToken.None);

        IReadOnlyList<string> ordered = DatabaseMigrator.GetOrderedMigrationResourceNames();

        int idx124 = ordered
            .Select(static (n, i) => (n, i))
            .First(static t => t.n.Contains("124_FindingRecords_FilterIndexes", StringComparison.OrdinalIgnoreCase))
            .i;

        DatabaseMigrator.RunExcludingTrailingScripts(catalogConnectionString, ordered.Count - idx124 - 1);

        string rollbackPath = Path.Combine(
            MigrationIntegrationSqlHelpers.ResolvePersistenceRollbackDirectory(),
            "R124_FindingRecords_FilterIndexes.sql");

        File.Exists(rollbackPath).Should().BeTrue(because: "rollback SQL must exist alongside forward migrations.");

        string rollbackSql = await File.ReadAllTextAsync(rollbackPath, CancellationToken.None);

        await MigrationIntegrationSqlHelpers.ExecuteGoBatchesAsync(catalogConnectionString, rollbackSql,
            CancellationToken.None);

        await using SqlConnection connection = new(catalogConnectionString);
        await connection.OpenAsync(CancellationToken.None);

        await using SqlCommand severity = new(
            """
            SELECT COUNT(*) FROM sys.indexes
            WHERE name = N'IX_FindingRecords_Snapshot_Severity'
              AND object_id = OBJECT_ID(N'dbo.FindingRecords');
            """,
            connection);

        Convert.ToInt32(await severity.ExecuteScalarAsync(CancellationToken.None), System.Globalization.CultureInfo.InvariantCulture).Should().Be(0);

        await using SqlCommand category = new(
            """
            SELECT COUNT(*) FROM sys.indexes
            WHERE name = N'IX_FindingRecords_Snapshot_Category'
              AND object_id = OBJECT_ID(N'dbo.FindingRecords');
            """,
            connection);

        Convert.ToInt32(await category.ExecuteScalarAsync(CancellationToken.None), System.Globalization.CultureInfo.InvariantCulture).Should().Be(0);
    }

    [SkippableFact]
    public async Task R128_Runs_RetrySupport_removes_columns_after_forward_migration()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        string suffix = Guid.NewGuid().ToString("N")[..10];
        string databaseName = "ArchLucidRb128_" + suffix;

        SqlConnectionStringBuilder builder =
            new(fixture.ConnectionString) { InitialCatalog = databaseName };

        string catalogConnectionString =
            SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(builder.ConnectionString);

        await SqlServerTestCatalogCommands.EnsureCatalogExistsAsync(catalogConnectionString, CancellationToken.None);

        IReadOnlyList<string> ordered = DatabaseMigrator.GetOrderedMigrationResourceNames();

        int idx128 = ordered
            .Select(static (n, i) => (n, i))
            .First(static t => t.n.Contains("128_Runs_RetrySupport", StringComparison.OrdinalIgnoreCase))
            .i;

        DatabaseMigrator.RunExcludingTrailingScripts(catalogConnectionString, ordered.Count - idx128 - 1);

        string rollbackPath = Path.Combine(
            MigrationIntegrationSqlHelpers.ResolvePersistenceRollbackDirectory(),
            "R128_Runs_RetrySupport.sql");

        File.Exists(rollbackPath).Should().BeTrue();

        string rollbackSql = await File.ReadAllTextAsync(rollbackPath, CancellationToken.None);

        await MigrationIntegrationSqlHelpers.ExecuteGoBatchesAsync(catalogConnectionString, rollbackSql,
            CancellationToken.None);

        await using SqlConnection connection = new(catalogConnectionString);
        await connection.OpenAsync(CancellationToken.None);

        await using SqlCommand retryCount = new(
            "SELECT COL_LENGTH(N'dbo.Runs', N'RetryCount');",
            connection);

        object? lenRetry = await retryCount.ExecuteScalarAsync(CancellationToken.None);

        lenRetry.Should().Be(DBNull.Value);

        await using SqlCommand lastFailure = new(
            "SELECT COL_LENGTH(N'dbo.Runs', N'LastFailureReason');",
            connection);

        object? lenFail = await lastFailure.ExecuteScalarAsync(CancellationToken.None);

        lenFail.Should().Be(DBNull.Value);
    }
}
