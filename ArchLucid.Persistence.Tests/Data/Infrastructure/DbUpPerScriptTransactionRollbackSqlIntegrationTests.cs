using ArchLucid.Persistence.Data.Infrastructure;
using ArchLucid.TestSupport;

using DbUp;
using DbUp.Engine;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Data.Infrastructure;

/// <summary>
///     Verifies DbUp <see cref="UpgradeEngine" /> with <see cref="UpgradeEngineBuilder.WithTransactionPerScript" />
///     rolls back earlier batches in the same script when a later batch fails â€” matching production
///     <see cref="DatabaseMigrator" /> behaviour.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DbUpPerScriptTransactionRollbackSqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    [Fact]
    public async Task Second_batch_failure_in_one_script_rolls_back_first_batch_DDL()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        string suffix = Guid.NewGuid().ToString("N")[..10];
        string databaseName = "ArchLucidDbUpTx_" + suffix;

        SqlConnectionStringBuilder builder =
            new(fixture.ConnectionString) { InitialCatalog = databaseName };

        string catalogConnectionString =
            SqlConnectionStringSecurity.EnsureSqlClientEncryptMandatory(builder.ConnectionString);

        await SqlServerTestCatalogCommands.EnsureCatalogExistsAsync(catalogConnectionString, CancellationToken.None);

        const string script = """
                              IF OBJECT_ID(N'dbo.DbUpFailureInjectionScratch', N'U') IS NOT NULL
                                  DROP TABLE dbo.DbUpFailureInjectionScratch;
                              GO
                              CREATE TABLE dbo.DbUpFailureInjectionScratch (Id INT NOT NULL PRIMARY KEY);
                              GO
                              RAISERROR(N'DbUp_failure_injection_second_batch', 16, 1);
                              GO
                              """;

        UpgradeEngine engine = DeployChanges.To
            .SqlDatabase(catalogConnectionString)
            .WithScripts(new SqlScript("failure-injection-test.sql", script))
            .WithTransactionPerScript()
            .JournalTo(new DbUp.Helpers.NullJournal())
            .Build();

        DatabaseUpgradeResult result = engine.PerformUpgrade();

        result.Successful.Should().BeFalse();

        await using SqlConnection connection = new(catalogConnectionString);
        await connection.OpenAsync(CancellationToken.None);

        await using SqlCommand command = new(
            "SELECT OBJECT_ID(N'dbo.DbUpFailureInjectionScratch', N'U');",
            connection);

        object? oid = await command.ExecuteScalarAsync(CancellationToken.None);

        oid.Should().Be(DBNull.Value, because: "CREATE TABLE from the first batch must roll back when the second batch RAISERROR fails.");
    }
}
