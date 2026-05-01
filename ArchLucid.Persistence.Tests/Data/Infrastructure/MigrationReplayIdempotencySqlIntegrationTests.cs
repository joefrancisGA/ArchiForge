using ArchLucid.Persistence.Data.Infrastructure;
using ArchLucid.TestSupport;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Data.Infrastructure;

/// <summary>
///     Verifies forward migrations guarded with <c>IF NOT EXISTS</c> / column-length checks stay safe when DbUp must
///     execute them again after journal repair (simulates deleting the journal row for one script).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class MigrationReplayIdempotencySqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    [Fact]
    public async Task DatabaseMigrator_re_applies_127_StateConstraints_after_journal_row_removed_without_error()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        string suffix = Guid.NewGuid().ToString("N")[..10];
        string databaseName = "ArchLucidReplay127_" + suffix;

        SqlConnectionStringBuilder builder =
            new(fixture.ConnectionString) { InitialCatalog = databaseName };

        string catalogConnectionString =
            SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(builder.ConnectionString);

        await SqlServerTestCatalogCommands.EnsureCatalogExistsAsync(catalogConnectionString, CancellationToken.None);

        DatabaseMigrator.Run(catalogConnectionString);

        await using (SqlConnection connection = new(catalogConnectionString))
        {
            await connection.OpenAsync(CancellationToken.None);

            await using SqlCommand delete = new(
                """
                DELETE FROM dbo.SchemaVersions
                WHERE ScriptName LIKE N'%127_StateConstraints_Batch%';
                """,
                connection);

            int deleted = await delete.ExecuteNonQueryAsync(CancellationToken.None);

            deleted.Should().Be(1);
        }

        Action replay = () => DatabaseMigrator.Run(catalogConnectionString);

        replay.Should().NotThrow();
    }

    [Fact]
    public async Task DatabaseMigrator_re_applies_129_RlsAuthorityChildTableScopeDenorm_after_journal_row_removed_without_error()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        string suffix = Guid.NewGuid().ToString("N")[..10];
        string databaseName = "ArchLucidReplay129_" + suffix;

        SqlConnectionStringBuilder builder =
            new(fixture.ConnectionString) { InitialCatalog = databaseName };

        string catalogConnectionString =
            SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(builder.ConnectionString);

        await SqlServerTestCatalogCommands.EnsureCatalogExistsAsync(catalogConnectionString, CancellationToken.None);

        DatabaseMigrator.Run(catalogConnectionString);

        await using (SqlConnection connection = new(catalogConnectionString))
        {
            await connection.OpenAsync(CancellationToken.None);

            await using SqlCommand delete = new(
                """
                DELETE FROM dbo.SchemaVersions
                WHERE ScriptName LIKE N'%129_RlsAuthorityChildTableScopeDenorm%';
                """,
                connection);

            int deleted = await delete.ExecuteNonQueryAsync(CancellationToken.None);

            deleted.Should().Be(1);
        }

        Action replay = () => DatabaseMigrator.Run(catalogConnectionString);

        replay.Should().NotThrow();
    }
}
