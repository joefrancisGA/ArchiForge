using System.Globalization;

using ArchLucid.Core.Tenancy;

using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Tenancy;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Tenancy;

[Trait("Category", "Integration")]
public sealed class TenantHardPurgeServiceSqlIntegrationTests : IClassFixture<SqlServerPersistenceFixture>
{
    private readonly SqlServerPersistenceFixture _fixture;

    public TenantHardPurgeServiceSqlIntegrationTests(SqlServerPersistenceFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task PurgeTenantAsync_removes_tenant_scoped_rows_and_retains_audit_events()
    {
        Skip.IfNot(_fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid auditEventId = Guid.NewGuid();

        await using (SqlConnection setup = new(_fixture.ConnectionString))
        {
            await setup.OpenAsync();

            await using SqlCommand tenantCmd = setup.CreateCommand();
            tenantCmd.CommandText = """
                                      INSERT INTO dbo.Tenants (Id, Name, Slug, Tier, TrialStatus, TrialExpiresUtc, TrialRunsUsed, TrialSeatsUsed)
                                      VALUES (@Id, N'purge-test', @Slug, N'Standard', N'Deleted', SYSUTCDATETIME(), 0, 1);
                                      """;
            tenantCmd.Parameters.AddWithValue("@Id", tenantId);
            tenantCmd.Parameters.AddWithValue("@Slug", $"purge-{tenantId:N}".ToLowerInvariant());
            await tenantCmd.ExecuteNonQueryAsync();

            await using SqlCommand wsCmd = setup.CreateCommand();
            wsCmd.CommandText = """
                                 INSERT INTO dbo.TenantWorkspaces (Id, TenantId, Name, DefaultProjectId)
                                 VALUES (@WsId, @TenantId, N'default', @ProjectId);
                                 """;
            wsCmd.Parameters.AddWithValue("@WsId", workspaceId);
            wsCmd.Parameters.AddWithValue("@TenantId", tenantId);
            wsCmd.Parameters.AddWithValue("@ProjectId", projectId);
            await wsCmd.ExecuteNonQueryAsync();

            await using SqlCommand usageCmd = setup.CreateCommand();
            usageCmd.CommandText = """
                                    INSERT INTO dbo.UsageEvents (TenantId, WorkspaceId, ProjectId, Kind, Quantity)
                                    VALUES (@TenantId, @WorkspaceId, @ProjectId, N'TestKind', 1);
                                    """;
            usageCmd.Parameters.AddWithValue("@TenantId", tenantId);
            usageCmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
            usageCmd.Parameters.AddWithValue("@ProjectId", projectId);
            await usageCmd.ExecuteNonQueryAsync();

            await using SqlCommand auditCmd = setup.CreateCommand();
            auditCmd.CommandText = """
                                    INSERT INTO dbo.AuditEvents (
                                        EventId, OccurredUtc, EventType, ActorUserId, ActorUserName,
                                        TenantId, WorkspaceId, ProjectId, DataJson)
                                    VALUES (
                                        @EventId, SYSUTCDATETIME(), N'PurgeFixture', N'test', N'Test User',
                                        @TenantId, @WorkspaceId, @ProjectId, N'{}');
                                    """;
            auditCmd.Parameters.AddWithValue("@EventId", auditEventId);
            auditCmd.Parameters.AddWithValue("@TenantId", tenantId);
            auditCmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
            auditCmd.Parameters.AddWithValue("@ProjectId", projectId);
            await auditCmd.ExecuteNonQueryAsync();
        }

        SqlConnectionFactory factory = new(_fixture.ConnectionString);
        SqlTenantHardPurgeService purge = new(factory);

        TenantHardPurgeResult result = await purge.PurgeTenantAsync(
            tenantId,
            new TenantHardPurgeOptions { DryRun = false, MaxRowsPerStatement = 500 },
            CancellationToken.None);

        result.RowsDeleted.Should().BeGreaterThan(0);

        await using SqlConnection verify = new(_fixture.ConnectionString);
        await verify.OpenAsync();

        await using SqlCommand tenantCount = verify.CreateCommand();
        tenantCount.CommandText = "SELECT COUNT(*) FROM dbo.Tenants WHERE Id = @Id;";
        tenantCount.Parameters.AddWithValue("@Id", tenantId);
        object? tScalar = await tenantCount.ExecuteScalarAsync();
        Convert.ToInt32(tScalar, CultureInfo.InvariantCulture).Should().Be(0);

        await using SqlCommand auditCount = verify.CreateCommand();
        auditCount.CommandText = "SELECT COUNT(*) FROM dbo.AuditEvents WHERE EventId = @EventId;";
        auditCount.Parameters.AddWithValue("@EventId", auditEventId);
        object? aScalar = await auditCount.ExecuteScalarAsync();
        Convert.ToInt32(aScalar, CultureInfo.InvariantCulture).Should().Be(1);
    }
}
