using System.Globalization;
using System.Net;

using ArchLucid.TestSupport;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Verifies the API host can start against a newly created empty SQL database and apply embedded migrations +
///     bootstrap.
///     Catches ordering bugs between DbUp and <c>SqlSchemaBootstrapper</c> that pre-migrated integration DBs would not
///     surface.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
[Collection("ArchLucidEnvMutation")]
public sealed class GreenfieldSqlBootIntegrationTests
{
    private const string SqlUnavailable =
        "API greenfield SQL tests need SQL Server. Set "
        + TestDatabaseEnvironment.ApiIntegrationSqlEnvironmentVariable
        + " or "
        + TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable
        + " (see docs/BUILD.md), or use Windows with LocalDB.";

    private static bool IsSqlServerConfiguredForApiIntegration()
    {
        if (!string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(TestDatabaseEnvironment.ApiIntegrationSqlEnvironmentVariable)))
            return true;

        if (!string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable)))
            return true;

        return OperatingSystem.IsWindows();
    }

    [SkippableFact]
    public async Task Api_boots_against_empty_database_and_health_ready_returns_ok()
    {
        Skip.IfNot(IsSqlServerConfiguredForApiIntegration(), SqlUnavailable);

        await using GreenfieldSqlApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task SchemaVersions_has_rows_after_greenfield_boot()
    {
        Skip.IfNot(IsSqlServerConfiguredForApiIntegration(), SqlUnavailable);

        await using GreenfieldSqlApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage ready = await client.GetAsync("/health/ready");

        ready.StatusCode.Should().Be(HttpStatusCode.OK);

        await using SqlConnection connection = new(factory.SqlConnectionString);
        await connection.OpenAsync();

        await using SqlCommand command = new(
            """
            SELECT COUNT(*) FROM dbo.SchemaVersions;
            """,
            connection);

        object? scalar = await command.ExecuteScalarAsync();

        scalar.Should().NotBeNull();
        Convert.ToInt32(scalar, CultureInfo.InvariantCulture).Should().BeGreaterThan(0);
    }

    [SkippableFact]
    public async Task Core_tables_exist_after_greenfield_boot()
    {
        Skip.IfNot(IsSqlServerConfiguredForApiIntegration(), SqlUnavailable);

        await using GreenfieldSqlApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage ready = await client.GetAsync("/health/ready");

        ready.StatusCode.Should().Be(HttpStatusCode.OK);

        await using SqlConnection connection = new(factory.SqlConnectionString);
        await connection.OpenAsync(CancellationToken.None);

        await AssertColumnLengthAsync(connection, "dbo.ArchitectureRequests", "RequestId");
        await AssertColumnLengthAsync(connection, "dbo.PolicyPackAssignments", "ArchivedUtc");
    }

    private static async Task AssertColumnLengthAsync(SqlConnection connection, string table, string column)
    {
        await using SqlCommand command = new(
            $"SELECT COL_LENGTH('{table}', '{column}') AS Len;",
            connection);

        object? scalar = await command.ExecuteScalarAsync(CancellationToken.None);

        scalar.Should().NotBe(DBNull.Value);
        Convert.ToInt32(scalar, CultureInfo.InvariantCulture).Should().BeGreaterThan(0);
    }
}
