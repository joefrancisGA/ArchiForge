using ArchLucid.TestSupport;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Boots <see cref="Program"/> with <c>ArchLucid:StorageProvider=Sql</c> against an **empty** SQL catalog (no DbUp journal).
/// Host startup must run DbUp then <c>ISchemaBootstrapper</c> — same path as greenfield deployments and CI <c>api-greenfield-boot</c>.
/// </summary>
public sealed class GreenfieldSqlApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    /// <summary>Creates the factory and ensures the catalog exists without applying migrations (host does that on boot).</summary>
    public GreenfieldSqlApiFactory()
    {
        try
        {
            string databaseName = "ArchLucidGreenfield_" + Guid.NewGuid().ToString("N");
            string raw = SqlServerIntegrationTestConnections.CreateEphemeralApiDatabaseConnectionString(databaseName);
            SqlConnectionStringBuilder builder = new(raw)
            {
                // Parallel integration tests (same host process) can open many connections at once; CI SQL is slower than local.
                MaxPoolSize = 200,
                ConnectTimeout = 120,
            };

            _connectionString = builder.ConnectionString;
            SqlServerTestCatalogCommands.EnsureCatalogExists(_connectionString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "GreenfieldSqlApiFactory could not prepare an ephemeral SQL catalog. "
                + "Set environment variable "
                + TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable
                + " or "
                + TestDatabaseEnvironment.ApiIntegrationSqlEnvironmentVariable
                + " to a reachable SQL Server (Linux/macOS require this). On Windows, ensure localhost accepts the connection. "
                + "See docs/BUILD.md (API integration tests).",
                ex);
        }
    }

    /// <summary>ADO.NET connection string for the empty catalog the API migrates on startup.</summary>
    public string SqlConnectionString => _connectionString;

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("ConnectionStrings:ArchLucid", _connectionString);
        builder.UseSetting("ArchLucid:StorageProvider", "Sql");
        builder.UseSetting("ArchLucidAuth:Mode", "DevelopmentBypass");
        builder.UseSetting("Authentication:ApiKey:DevelopmentBypassAll", "true");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ArchLucid:StorageProvider"] = "Sql",
                ["ConnectionStrings:ArchLucid"] = _connectionString,
                ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
                ["Authentication:ApiKey:DevelopmentBypassAll"] = "true",
                ["AgentExecution:Mode"] = "Simulator",
                ["AzureOpenAI:Endpoint"] = "",
                ["AzureOpenAI:ApiKey"] = "",
                ["AzureOpenAI:DeploymentName"] = "",
                ["AzureOpenAI:EmbeddingDeploymentName"] = "",
                ["RateLimiting:FixedWindow:PermitLimit"] = "100000",
                ["RateLimiting:FixedWindow:WindowMinutes"] = "1",
                ["RateLimiting:Expensive:PermitLimit"] = "100000",
                ["RateLimiting:Expensive:WindowMinutes"] = "1",
                ["RateLimiting:Replay:Light:PermitLimit"] = "100000",
                ["RateLimiting:Replay:Heavy:PermitLimit"] = "100000",
                ["RateLimiting:Registration:PermitLimit"] = "100000",
                ["RateLimiting:Registration:WindowMinutes"] = "1",
            });
        });
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        try
        {
            SqlServerTestCatalogCommands.DropCatalogIfExists(_connectionString);
        }
        catch
        {
            // Best-effort cleanup (SQL Server may be unavailable on teardown).
        }
    }
}
