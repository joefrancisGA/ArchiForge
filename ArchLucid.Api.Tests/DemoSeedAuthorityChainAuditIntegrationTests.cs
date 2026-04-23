using System.Globalization;
using System.Net;

using ArchLucid.Application.Bootstrap;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.TestSupport;

using FluentAssertions;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace ArchLucid.Api.Tests;

/// <summary>
/// <c>dbo.AuditEvents</c> exists only after DbUp on a SQL-backed catalog. <see cref="ArchLucidApiFactory"/> forces
/// <c>ArchLucid:StorageProvider=InMemory</c>, so authority-chain audit SQL assertions belong on <see cref="GreenfieldSqlApiFactory"/>.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
[Collection("ArchLucidEnvMutation")]
public sealed class DemoSeedAuthorityChainAuditIntegrationTests
{
    private const string SqlUnavailable =
        "API greenfield SQL tests need SQL Server. Set "
        + TestDatabaseEnvironment.ApiIntegrationSqlEnvironmentVariable
        + " or "
        + TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable
        + " (see docs/BUILD.md), or use Windows with LocalDB.";

    private static bool IsSqlServerConfiguredForApiIntegration()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(TestDatabaseEnvironment.ApiIntegrationSqlEnvironmentVariable)))
            return true;

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable)))
            return true;

        return OperatingSystem.IsWindows();
    }

    [SkippableFact]
    public async Task SeedAsync_authority_chain_audit_is_idempotent_against_sql()
    {
        Skip.IfNot(IsSqlServerConfiguredForApiIntegration(), SqlUnavailable);

        await using GreenfieldSqlApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        HttpResponseMessage ready = await client.GetAsync("/health/ready");

        ready.StatusCode.Should().Be(HttpStatusCode.OK);

        Guid tenantId;

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            tenantId = scope.ServiceProvider.GetRequiredService<IScopeContextProvider>().GetCurrentScope().TenantId;
            await scope.ServiceProvider.GetRequiredService<IDemoSeedService>().SeedAsync();
        }

        int afterFirst = await CountAuthorityCommittedChainAuditRowsAsync(factory.SqlConnectionString, tenantId);
        afterFirst.Should().Be(2);

        using (IServiceScope scope2 = factory.Services.CreateScope())
        {
            await scope2.ServiceProvider.GetRequiredService<IDemoSeedService>().SeedAsync();
        }

        int afterSecond = await CountAuthorityCommittedChainAuditRowsAsync(factory.SqlConnectionString, tenantId);
        afterSecond.Should().Be(2);

        await using SqlConnection connection = new(factory.SqlConnectionString);
        await connection.OpenAsync(CancellationToken.None);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT TOP (1) CorrelationId
            FROM dbo.AuditEvents
            WHERE TenantId = @tenantId AND EventType = @eventType
            ORDER BY OccurredUtc DESC
            """;
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@eventType", AuditEventTypes.AuthorityCommittedChainPersisted);
        object? correlation = await command.ExecuteScalarAsync(CancellationToken.None);
        correlation.Should().NotBeNull();
        correlation!.ToString().Should().NotBeNullOrWhiteSpace();
    }

    private static async Task<int> CountAuthorityCommittedChainAuditRowsAsync(string connectionString, Guid tenantId)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(CancellationToken.None);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(*)
            FROM dbo.AuditEvents
            WHERE TenantId = @tenantId AND EventType = @eventType
            """;
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@eventType", AuditEventTypes.AuthorityCommittedChainPersisted);
        object? scalar = await command.ExecuteScalarAsync(CancellationToken.None);

        return Convert.ToInt32(scalar, CultureInfo.InvariantCulture);
    }
}
