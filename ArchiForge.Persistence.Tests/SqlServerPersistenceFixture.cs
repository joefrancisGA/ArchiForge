using ArchiForge.Data.Infrastructure;

using Microsoft.Data.SqlClient;

using Testcontainers.MsSql;

namespace ArchiForge.Persistence.Tests;

/// <summary>
/// Spins up <see cref="MsSqlContainer"/>, applies embedded <see cref="DatabaseMigrator"/> scripts (same path as API startup on SQL Server),
/// and exposes a connection string for Dapper repositories under <c>ArchiForge.Persistence</c>.
/// </summary>
/// <remarks>
/// Requires Docker. Filter out with <c>dotnet test --filter "Category!=SqlServerContainer"</c> when Docker is unavailable.
/// </remarks>
public sealed class SqlServerPersistenceFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
        .WithPassword("ArchiForge_Tc_Test_2026!")
        .Build();

    /// <summary>Connection string after <see cref="InitializeAsync"/>; includes <c>TrustServerCertificate=True</c> for local SqlClient.</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);

        SqlConnectionStringBuilder builder = new(_container.GetConnectionString())
        {
            TrustServerCertificate = true
        };
        ConnectionString = builder.ConnectionString;

        if (!DatabaseMigrator.Run(ConnectionString))
        {
            throw new InvalidOperationException(
                "DbUp failed against Testcontainers SQL Server; see test output for script errors.");
        }
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync().ConfigureAwait(false);
    }
}
