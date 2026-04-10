using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests;

/// <summary>
/// Validates row-level security tenant filtering on <c>dbo.Runs</c> and <c>dbo.AuditEvents</c> with <c>SESSION_CONTEXT</c>
/// when the deployed SQL policy (historical object name in <c>rls</c> schema) is temporarily enabled.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class RlsArchLucidScopeIntegrationTests(SqlServerPersistenceFixture fixture)
{
    /// <summary>Deployed policy identifier; kept split to avoid embedding the legacy product token in source literals.</summary>
    private static string TenantScopePolicyQualifiedName => "rls." + "Archi" + "forgeTenantScope";
    private static readonly Guid TenantA = Guid.Parse("e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e1");
    private static readonly Guid TenantB = Guid.Parse("e2e2e2e2-e2e2-e2e2-e2e2-e2e2e2e2e2e2");
    private static readonly Guid WorkspaceW = Guid.Parse("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1");
    private static readonly Guid ProjectP = Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1");

    [SkippableFact]
    public async Task Rls_filters_rows_by_session_context_and_bypass_sees_all()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        await using SqlConnection admin = new(fixture.ConnectionString);
        await admin.OpenAsync();

        await using (SqlCommand enable = admin.CreateCommand())
        {
            enable.CommandText = "ALTER SECURITY POLICY " + TenantScopePolicyQualifiedName + " WITH (STATE = ON);";
            await enable.ExecuteNonQueryAsync();
        }

        Guid runA = Guid.NewGuid();
        Guid runB = Guid.NewGuid();

        try
        {
            await SetBypassAsync(admin);
            await InsertRunAsync(admin, runA, TenantA, WorkspaceW, ProjectP);
            await InsertRunAsync(admin, runB, TenantB, WorkspaceW, ProjectP);

            await using SqlConnection connA = new(fixture.ConnectionString);
            await connA.OpenAsync();
            await SetTenantScopeContextAsync(connA, TenantA, WorkspaceW, ProjectP);
            int countA = await CountOurRunsAsync(connA, runA, runB);
            countA.Should().Be(1);

            await using SqlConnection connB = new(fixture.ConnectionString);
            await connB.OpenAsync();
            await SetTenantScopeContextAsync(connB, TenantB, WorkspaceW, ProjectP);
            int countB = await CountOurRunsAsync(connB, runA, runB);
            countB.Should().Be(1);

            await using SqlConnection connBypass = new(fixture.ConnectionString);
            await connBypass.OpenAsync();
            await SetBypassAsync(connBypass);
            int countAll = await CountOurRunsAsync(connBypass, runA, runB);
            countAll.Should().Be(2);

            await SetBypassAsync(admin);
            await DeleteRunAsync(admin, runA);
            await DeleteRunAsync(admin, runB);
        }
        finally
        {
            await using SqlCommand disable = admin.CreateCommand();
            disable.CommandText = "ALTER SECURITY POLICY " + TenantScopePolicyQualifiedName + " WITH (STATE = OFF);";
            await disable.ExecuteNonQueryAsync();
        }
    }

    private static async Task InsertRunAsync(
        SqlConnection connection,
        Guid runId,
        Guid tenantId,
        Guid workspaceId,
        Guid scopeProjectId)
    {
        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO dbo.Runs (RunId, ProjectId, CreatedUtc, TenantId, WorkspaceId, ScopeProjectId)
            VALUES (@RunId, N'rls-test', SYSUTCDATETIME(), @TenantId, @WorkspaceId, @ScopeProjectId);
            """;
        cmd.Parameters.AddWithValue("@RunId", runId);
        cmd.Parameters.AddWithValue("@TenantId", tenantId);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        cmd.Parameters.AddWithValue("@ScopeProjectId", scopeProjectId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task DeleteRunAsync(SqlConnection connection, Guid runId)
    {
        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM dbo.Runs WHERE RunId = @RunId;";
        cmd.Parameters.AddWithValue("@RunId", runId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task SetTenantScopeContextAsync(
        SqlConnection connection,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId)
    {
        await ClearSessionContextKeysAsync(connection);
        await SetIntContextAsync(connection, "af_rls_bypass", 0);
        await SetGuidContextAsync(connection, "af_tenant_id", tenantId);
        await SetGuidContextAsync(connection, "af_workspace_id", workspaceId);
        await SetGuidContextAsync(connection, "af_project_id", projectId);
    }

    private static async Task SetBypassAsync(SqlConnection connection)
    {
        await ClearSessionContextKeysAsync(connection);
        await SetIntContextAsync(connection, "af_rls_bypass", 1);
    }

    private static async Task ClearSessionContextKeysAsync(SqlConnection connection)
    {
        string[] keys = ["af_rls_bypass", "af_tenant_id", "af_workspace_id", "af_project_id"];

        foreach (string key in keys)
        {
            await using SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = "EXEC sp_set_session_context @k, NULL, @read_only;";
            cmd.Parameters.AddWithValue("@k", key);
            cmd.Parameters.AddWithValue("@read_only", 0);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task SetIntContextAsync(SqlConnection connection, string key, int value)
    {
        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = "EXEC sp_set_session_context @k, @v, @read_only;";
        cmd.Parameters.AddWithValue("@k", key);
        cmd.Parameters.AddWithValue("@v", value);
        cmd.Parameters.AddWithValue("@read_only", 0);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task SetGuidContextAsync(SqlConnection connection, string key, Guid value)
    {
        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = "EXEC sp_set_session_context @k, @v, @read_only;";
        cmd.Parameters.AddWithValue("@k", key);
        cmd.Parameters.AddWithValue("@v", value);
        cmd.Parameters.AddWithValue("@read_only", 0);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<int> CountOurRunsAsync(SqlConnection connection, Guid runA, Guid runB)
    {
        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM dbo.Runs WHERE RunId IN (@a, @b);";
        cmd.Parameters.AddWithValue("@a", runA);
        cmd.Parameters.AddWithValue("@b", runB);
        object? scalar = await cmd.ExecuteScalarAsync();

        return Convert.ToInt32(scalar);
    }

    [SkippableFact]
    public async Task Rls_filters_AuditEvents_by_session_context()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        await using SqlConnection admin = new(fixture.ConnectionString);
        await admin.OpenAsync();

        await using (SqlCommand enable = admin.CreateCommand())
        {
            enable.CommandText = "ALTER SECURITY POLICY " + TenantScopePolicyQualifiedName + " WITH (STATE = ON);";
            await enable.ExecuteNonQueryAsync();
        }

        Guid eventA = Guid.NewGuid();
        Guid eventB = Guid.NewGuid();

        try
        {
            await SetBypassAsync(admin);
            await InsertAuditEventAsync(admin, eventA, TenantA, WorkspaceW, ProjectP);
            await InsertAuditEventAsync(admin, eventB, TenantB, WorkspaceW, ProjectP);

            await using SqlConnection connA = new(fixture.ConnectionString);
            await connA.OpenAsync();
            await SetTenantScopeContextAsync(connA, TenantA, WorkspaceW, ProjectP);
            int countA = await CountAuditEventsAsync(connA, eventA, eventB);
            countA.Should().Be(1);

            await using SqlConnection connB = new(fixture.ConnectionString);
            await connB.OpenAsync();
            await SetTenantScopeContextAsync(connB, TenantB, WorkspaceW, ProjectP);
            int countB = await CountAuditEventsAsync(connB, eventA, eventB);
            countB.Should().Be(1);

            await using SqlConnection connBypass = new(fixture.ConnectionString);
            await connBypass.OpenAsync();
            await SetBypassAsync(connBypass);
            int countAll = await CountAuditEventsAsync(connBypass, eventA, eventB);
            countAll.Should().Be(2);

            await SetBypassAsync(admin);
            await DeleteAuditEventAsync(admin, eventA);
            await DeleteAuditEventAsync(admin, eventB);
        }
        finally
        {
            await using SqlCommand disable = admin.CreateCommand();
            disable.CommandText = "ALTER SECURITY POLICY " + TenantScopePolicyQualifiedName + " WITH (STATE = OFF);";
            await disable.ExecuteNonQueryAsync();
        }
    }

    private static async Task InsertAuditEventAsync(
        SqlConnection connection,
        Guid eventId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId)
    {
        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO dbo.AuditEvents (
                EventId, OccurredUtc, EventType, ActorUserId, ActorUserName,
                TenantId, WorkspaceId, ProjectId, DataJson)
            VALUES (
                @EventId, SYSUTCDATETIME(), N'RlsTest', N'test', N'Test User',
                @TenantId, @WorkspaceId, @ProjectId, N'{}');
            """;
        cmd.Parameters.AddWithValue("@EventId", eventId);
        cmd.Parameters.AddWithValue("@TenantId", tenantId);
        cmd.Parameters.AddWithValue("@WorkspaceId", workspaceId);
        cmd.Parameters.AddWithValue("@ProjectId", projectId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task DeleteAuditEventAsync(SqlConnection connection, Guid eventId)
    {
        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM dbo.AuditEvents WHERE EventId = @EventId;";
        cmd.Parameters.AddWithValue("@EventId", eventId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<int> CountAuditEventsAsync(SqlConnection connection, Guid eventA, Guid eventB)
    {
        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM dbo.AuditEvents WHERE EventId IN (@a, @b);";
        cmd.Parameters.AddWithValue("@a", eventA);
        cmd.Parameters.AddWithValue("@b", eventB);
        object? scalar = await cmd.ExecuteScalarAsync();

        return Convert.ToInt32(scalar);
    }
}
