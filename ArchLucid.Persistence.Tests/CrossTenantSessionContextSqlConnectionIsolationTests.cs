using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Tests.Support;

using FluentAssertions;

using Microsoft.Data.SqlClient;

using Microsoft.Extensions.Logging.Abstractions;

namespace ArchLucid.Persistence.Tests;

/// <summary>
/// Cross-tenant isolation using the production-style stack:
/// <see cref="SqlConnectionFactory"/> → <see cref="ResilientSqlConnectionFactory"/> → <see cref="SessionContextSqlConnectionFactory"/>
/// with <see cref="RlsSessionContextApplicator"/> and <see cref="IOptionsMonitor{SqlServerOptions}"/>.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class CrossTenantSessionContextSqlConnectionIsolationTests(SqlServerPersistenceFixture fixture)
{
    private static string TenantScopePolicyQualifiedName => "rls.ArchLucidTenantScope";
    private static readonly Guid TenantA = Guid.Parse("c1c1c1c1-c1c1-c1c1-c1c1-c1c1c1c1c1c1");
    private static readonly Guid TenantB = Guid.Parse("c2c2c2c2-c2c2-c2c2-c2c2-c2c2c2c2c2c2");
    private static readonly Guid WorkspaceW = Guid.Parse("d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1");
    private static readonly Guid ProjectP = Guid.Parse("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1");

    [SkippableFact]
    public async Task SessionContext_factory_path_filters_runs_per_tenant_scope()
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

            MutableScopeContextProvider scopeProvider = new();
            SqlServerOptions sqlOptions = new()
            {
                RowLevelSecurity = new SqlRowLevelSecuritySettings { ApplySessionContext = true },
            };
            FixedSqlServerOptionsMonitor optionsMonitor = new(sqlOptions);
            RlsSessionContextApplicator applicator = new(scopeProvider, optionsMonitor);
            ResilientSqlConnectionFactory resilient = new(
                new SqlConnectionFactory(fixture.ConnectionString),
                SqlOpenResilienceDefaults.BuildSqlOpenRetryPipeline(maxRetryAttempts: 0));
            SessionContextSqlConnectionFactory factory = new(
                resilient,
                applicator,
                NullLogger<SessionContextSqlConnectionFactory>.Instance);

            scopeProvider.Current = new ScopeContext
            {
                TenantId = TenantA,
                WorkspaceId = WorkspaceW,
                ProjectId = ProjectP,
            };

            await using (SqlConnection connA = await factory.CreateOpenConnectionAsync(CancellationToken.None))
            {
                int countA = await CountOurRunsAsync(connA, runA, runB);
                countA.Should().Be(1);
            }

            scopeProvider.Current = new ScopeContext
            {
                TenantId = TenantB,
                WorkspaceId = WorkspaceW,
                ProjectId = ProjectP,
            };

            await using (SqlConnection connB = await factory.CreateOpenConnectionAsync(CancellationToken.None))
            {
                int countB = await CountOurRunsAsync(connB, runA, runB);
                countB.Should().Be(1);
            }

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
            VALUES (@RunId, N'rls-session-factory-test', SYSUTCDATETIME(), @TenantId, @WorkspaceId, @ScopeProjectId);
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

    private static async Task SetBypassAsync(SqlConnection connection)
    {
        await ClearSessionContextKeysAsync(connection);
        await SetIntContextAsync(connection, "al_rls_bypass", 1);
    }

    private static async Task ClearSessionContextKeysAsync(SqlConnection connection)
    {
        string[] keys = ["al_rls_bypass", "al_tenant_id", "al_workspace_id", "al_project_id"];

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

    private static async Task<int> CountOurRunsAsync(SqlConnection connection, Guid runA, Guid runB)
    {
        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM dbo.Runs WHERE RunId IN (@a, @b);";
        cmd.Parameters.AddWithValue("@a", runA);
        cmd.Parameters.AddWithValue("@b", runB);
        object? scalar = await cmd.ExecuteScalarAsync();

        return Convert.ToInt32(scalar);
    }
}
