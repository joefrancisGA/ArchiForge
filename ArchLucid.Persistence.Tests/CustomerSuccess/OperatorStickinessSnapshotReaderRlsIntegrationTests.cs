using ArchLucid.Core.CustomerSuccess;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.CustomerSuccess;
using ArchLucid.Persistence.Tests.Support;

using FluentAssertions;

using Microsoft.Data.SqlClient;

using Moq;

namespace ArchLucid.Persistence.Tests.CustomerSuccess;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class OperatorStickinessSnapshotReaderRlsIntegrationTests(SqlServerPersistenceFixture fixture)
{
    private static readonly Guid TenantA = Guid.Parse("d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1");
    private static readonly Guid TenantB = Guid.Parse("d2d2d2d2-d2d2-d2d2-d2d2-d2d2d2d2d2d2");
    private static readonly Guid WorkspaceW = Guid.Parse("e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e1");
    private static readonly Guid ProjectP = Guid.Parse("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1");

    private static string TenantScopePolicyQualifiedName => "rls.ArchLucidTenantScope";

    [SkippableFact]
    public async Task GetOperatorSignalsAsync_with_session_tenantA_cannot_read_tenantB_run_counts()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        await using SqlConnection admin = new(fixture.ConnectionString);
        await admin.OpenAsync();

        await using (SqlCommand enable = admin.CreateCommand())
        {
            enable.CommandText = "ALTER SECURITY POLICY " + TenantScopePolicyQualifiedName + " WITH (STATE = ON);";
            await enable.ExecuteNonQueryAsync();
        }

        Guid runB = Guid.NewGuid();

        try
        {
            await SetBypassAsync(admin);
            await InsertRunAsync(admin, runB, TenantB, WorkspaceW, ProjectP);

            FilteredScopeSqlConnectionFactory scopedFactory = new(fixture.ConnectionString, TenantA, WorkspaceW, ProjectP);
            Mock<IRlsSessionContextApplicator> rls = new();
            rls.Setup(a => a.ApplyAsync(It.IsAny<SqlConnection>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            SqlOperatorStickinessSnapshotReader sut = new(scopedFactory, rls.Object);

            OperatorStickinessSignals wrongSession =
                await sut.GetOperatorSignalsAsync(TenantB, WorkspaceW, ProjectP, CancellationToken.None);

            wrongSession.TotalRunsInScope.Should().Be(0);

            FilteredScopeSqlConnectionFactory matchFactory = new(fixture.ConnectionString, TenantB, WorkspaceW, ProjectP);
            SqlOperatorStickinessSnapshotReader sutB = new(matchFactory, rls.Object);

            OperatorStickinessSignals match = await sutB.GetOperatorSignalsAsync(TenantB, WorkspaceW, ProjectP, CancellationToken.None);

            match.TotalRunsInScope.Should().Be(1);

            await SetBypassAsync(admin);
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
                          VALUES (@RunId, N'sticky-rls', SYSUTCDATETIME(), @TenantId, @WorkspaceId, @ScopeProjectId);
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
        await PersistenceIntegrationTestRlsSession.ApplyArchLucidRlsBypassAsync(connection, CancellationToken.None);
    }

    private sealed class FilteredScopeSqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connectionString;
        private readonly Guid _tenantId;
        private readonly Guid _workspaceId;
        private readonly Guid _projectId;

        public FilteredScopeSqlConnectionFactory(
            string connectionString,
            Guid tenantId,
            Guid workspaceId,
            Guid projectId)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _tenantId = tenantId;
            _workspaceId = workspaceId;
            _projectId = projectId;
        }

        public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct)
        {
            SqlConnection connection = new(_connectionString);
            await connection.OpenAsync(ct);

            await PersistenceIntegrationTestRlsSession.ApplyArchLucidTenantScopeFilterOnlyAsync(
                connection,
                ct,
                _tenantId,
                _workspaceId,
                _projectId);

            return connection;
        }
    }
}
