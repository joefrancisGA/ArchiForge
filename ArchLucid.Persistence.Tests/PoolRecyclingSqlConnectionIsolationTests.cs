using ArchLucid.Core.Scoping;

using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Tests.Support;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     Forces ADO.NET connection-pool reuse (<c>Max Pool Size=1</c>) so recycled physical connections must have
///     <c>SESSION_CONTEXT</c> overwritten by <see cref="SessionContextSqlConnectionFactory" /> before use.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class PoolRecyclingSqlConnectionIsolationTests(SqlServerPersistenceFixture fixture)
{
    private static readonly Guid TenantA = Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1");
    private static readonly Guid TenantB = Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2");
    private static readonly Guid WorkspaceW = Guid.Parse("c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3");
    private static readonly Guid ProjectP = Guid.Parse("d4d4d4d4-d4d4-d4d4-d4d4-d4d4d4d4d4d4");

    private static string TenantScopePolicyQualifiedName => "rls.ArchLucidTenantScope";

    [SkippableFact]
    public async Task Recycled_connection_overwrites_stale_tenant_session_context()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        SqlConnection.ClearAllPools();

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
            SessionContextSqlConnectionFactory factory = CreateSinglePoolFactory(fixture.ConnectionString, scopeProvider);

            scopeProvider.Current = new ScopeContext
            {
                TenantId = TenantA, WorkspaceId = WorkspaceW, ProjectId = ProjectP
            };

            await using (SqlConnection connA = await factory.CreateOpenConnectionAsync(CancellationToken.None))
            {
                int countA = await CountOurRunsAsync(connA, runA, runB);
                countA.Should().Be(1);
            }

            scopeProvider.Current = new ScopeContext
            {
                TenantId = TenantB, WorkspaceId = WorkspaceW, ProjectId = ProjectP
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

    [SkippableFact]
    public async Task Recycled_connection_overwrites_stale_bypass_session_context()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        SqlConnection.ClearAllPools();

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
            SessionContextSqlConnectionFactory factory = CreateSinglePoolFactory(fixture.ConnectionString, scopeProvider);

            using (SqlRowLevelSecurityBypassAmbient.Enter())
            {
                await using (SqlConnection bypassConn = await factory.CreateOpenConnectionAsync(CancellationToken.None))
                {
                    int bothVisible = await CountOurRunsAsync(bypassConn, runA, runB);
                    bothVisible.Should().Be(2);
                }
            }

            scopeProvider.Current = new ScopeContext
            {
                TenantId = TenantA, WorkspaceId = WorkspaceW, ProjectId = ProjectP
            };

            await using (SqlConnection tenantConn = await factory.CreateOpenConnectionAsync(CancellationToken.None))
            {
                int tenantOnly = await CountOurRunsAsync(tenantConn, runA, runB);
                tenantOnly.Should().Be(1);
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

    private static SessionContextSqlConnectionFactory CreateSinglePoolFactory(
        string baseConnectionString,
        MutableScopeContextProvider scopeProvider)
    {
        SqlConnectionStringBuilder builder = new(baseConnectionString)
        {
            MaxPoolSize = 1,
            ApplicationName = "ArchLucid.PoolRecyclingSqlConnectionIsolationTests"
        };

        SqlServerOptions sqlOptions = new()
        {
            RowLevelSecurity = new SqlRowLevelSecuritySettings { ApplySessionContext = true }
        };

        FixedSqlServerOptionsMonitor optionsMonitor = new(sqlOptions);
        RlsSessionContextApplicator applicator = new(scopeProvider, optionsMonitor);
        ResilientSqlConnectionFactory resilient = new(
            new SqlConnectionFactory(builder.ConnectionString),
            SqlOpenResilienceDefaults.BuildSqlOpenRetryPipeline(maxRetryAttempts: 0));

        return new SessionContextSqlConnectionFactory(
            resilient,
            applicator,
            NullLogger<SessionContextSqlConnectionFactory>.Instance);
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
                          VALUES (@RunId, N'pool-recycle-test', SYSUTCDATETIME(), @TenantId, @WorkspaceId, @ScopeProjectId);
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
