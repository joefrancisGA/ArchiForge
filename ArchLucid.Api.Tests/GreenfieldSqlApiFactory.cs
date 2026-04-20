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
public class GreenfieldSqlApiFactory : WebApplicationFactory<Program>
{
    private const string ArchLucidPersistenceAllowRlsBypassEnvKey = "ArchLucid__Persistence__AllowRlsBypass";

    private static readonly object RlsBreakGlassEnvLock = new();

    private static int _rlsBreakGlassEnvRefCount;

    private static string? _savedArchLucidAllowRlsBypassEnv;

    private static string? _savedArchLucidPersistenceAllowRlsBypassEnv;

    private bool _rlsBreakGlassEnvLease;

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

            SqlConnectionString = builder.ConnectionString;
            SqlServerTestCatalogCommands.EnsureCatalogExists(SqlConnectionString);
            AcquireRlsBreakGlassEnvLease();
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
    public string SqlConnectionString
    {
        get;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("ConnectionStrings:ArchLucid", SqlConnectionString);
        builder.UseSetting("ArchLucid:StorageProvider", "Sql");
        builder.UseSetting("ArchLucid:Persistence:AllowRlsBypass", "true");
        builder.UseSetting("ArchLucidAuth:Mode", "DevelopmentBypass");
        builder.UseSetting("Authentication:ApiKey:DevelopmentBypassAll", "true");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ArchLucid:StorageProvider"] = "Sql",
                ["ConnectionStrings:ArchLucid"] = SqlConnectionString,
                ["ArchLucid:Persistence:AllowRlsBypass"] = "true",
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

        ReleaseRlsBreakGlassEnvLease();

        try
        {
            SqlServerTestCatalogCommands.DropCatalogIfExists(SqlConnectionString);
        }
        catch
        {
            // Best-effort cleanup (SQL Server may be unavailable on teardown).
        }
    }

    private void AcquireRlsBreakGlassEnvLease()
    {
        if (_rlsBreakGlassEnvLease)
            return;

        lock (RlsBreakGlassEnvLock)
        {
            if (_rlsBreakGlassEnvRefCount++ == 0)
            {
                _savedArchLucidAllowRlsBypassEnv = Environment.GetEnvironmentVariable("ARCHLUCID_ALLOW_RLS_BYPASS");
                _savedArchLucidPersistenceAllowRlsBypassEnv = Environment.GetEnvironmentVariable(ArchLucidPersistenceAllowRlsBypassEnvKey);
                Environment.SetEnvironmentVariable("ARCHLUCID_ALLOW_RLS_BYPASS", "true");
                Environment.SetEnvironmentVariable(ArchLucidPersistenceAllowRlsBypassEnvKey, "true");
            }

            _rlsBreakGlassEnvLease = true;
        }
    }

    private void ReleaseRlsBreakGlassEnvLease()
    {
        if (!_rlsBreakGlassEnvLease)
            return;

        lock (RlsBreakGlassEnvLock)
        {
            _rlsBreakGlassEnvLease = false;

            if (--_rlsBreakGlassEnvRefCount != 0)
                return;

            if (_savedArchLucidAllowRlsBypassEnv is null)
                Environment.SetEnvironmentVariable("ARCHLUCID_ALLOW_RLS_BYPASS", null);
            else
                Environment.SetEnvironmentVariable("ARCHLUCID_ALLOW_RLS_BYPASS", _savedArchLucidAllowRlsBypassEnv);

            if (_savedArchLucidPersistenceAllowRlsBypassEnv is null)
                Environment.SetEnvironmentVariable(ArchLucidPersistenceAllowRlsBypassEnvKey, null);
            else
                Environment.SetEnvironmentVariable(ArchLucidPersistenceAllowRlsBypassEnvKey, _savedArchLucidPersistenceAllowRlsBypassEnv);

            _savedArchLucidAllowRlsBypassEnv = null;
            _savedArchLucidPersistenceAllowRlsBypassEnv = null;
        }
    }
}
