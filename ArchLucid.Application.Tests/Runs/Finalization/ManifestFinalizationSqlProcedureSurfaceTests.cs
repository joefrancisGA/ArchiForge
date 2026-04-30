using ArchLucid.TestSupport;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Application.Tests.Runs.Finalization;

/// <summary>
///     Lightweight SQL checks for finalization stored procedures (skipped unless <c>ARCHLUCID_SQL_TEST</c> is set).
/// </summary>
[Trait("Category", "SqlIntegration")]
public sealed class ManifestFinalizationSqlProcedureSurfaceTests
{
    /// <summary>
    ///     Confirms <c>dbo.sp_FinalizeManifest</c> is deployed to the configured catalog (CI / local SQL regression).
    /// </summary>
    [SkippableFact]
    public async Task dbo_sp_FinalizeManifest_exists_when_sql_catalog_configured()
    {
        string? raw = Environment.GetEnvironmentVariable(TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable);

        Skip.If(
            string.IsNullOrWhiteSpace(raw),
            "Set " + TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable + " to run this SQL integration test.");

        SqlConnectionStringBuilder builder = new(raw.Trim())
        {
            Encrypt = SqlConnectionEncryptOption.Mandatory,
            TrustServerCertificate = true
        };

        await using SqlConnection connection = new(builder.ConnectionString);
        await connection.OpenAsync();

        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(1) FROM sys.procedures WHERE schema_id = SCHEMA_ID(N'dbo') AND name = N'sp_FinalizeManifest';";

        object? scalar = await command.ExecuteScalarAsync(CancellationToken.None);
        int count = Convert.ToInt32(scalar);

        count.Should().Be(1);
    }
}
