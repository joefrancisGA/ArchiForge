using System.Data;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Support;

/// <summary>
/// Minimal <c>dbo.ArchitectureRequests</c> and <c>dbo.Runs</c> rows for Data-layer SQL contract tests (logical run header).
/// </summary>
public static class ArchitectureCommitTestSeed
{
    /// <summary>Aligned with SQL contract idempotency tests' tenant/workspace/project GUIDs.</summary>
    private static readonly Guid SeedTenantId = Guid.Parse("10101010-1010-1010-1010-101010101010");

    private static readonly Guid SeedWorkspaceId = Guid.Parse("20202020-2020-2020-2020-202020202020");

    private static readonly Guid SeedScopeProjectId = Guid.Parse("30303030-3030-3030-3030-303030303030");

    /// <summary>Inserts <c>dbo.ArchitectureRequests</c> only (idempotent on <paramref name="requestId"/>).</summary>
    public static async Task InsertArchitectureRequestOnlyAsync(
        SqlConnection connection,
        string requestId,
        string systemName,
        CancellationToken ct)
    {
        const string insertRequest = """
            IF NOT EXISTS (SELECT 1 FROM dbo.ArchitectureRequests WHERE RequestId = @RequestId)
            INSERT INTO dbo.ArchitectureRequests
            (RequestId, SystemName, Environment, CloudProvider, RequestJson, CreatedUtc)
            VALUES (@RequestId, @SystemName, @Environment, @CloudProvider, @RequestJson, SYSUTCDATETIME());
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertRequest,
                new
                {
                    RequestId = requestId,
                    SystemName = systemName,
                    Environment = "prod",
                    CloudProvider = "Azure",
                    RequestJson = "{}",
                },
                cancellationToken: ct));
    }

    /// <summary>Inserts request + authority <c>dbo.Runs</c> row (idempotent on <paramref name="requestId"/> / <paramref name="runId"/>).</summary>
    public static async Task InsertRequestAndRunAsync(
        SqlConnection connection,
        string requestId,
        string runId,
        CancellationToken ct)
    {
        if (!Guid.TryParseExact(runId, "N", out Guid runGuid))
        {
            throw new ArgumentException("runId must be a 32-character hexadecimal GUID (N format).", nameof(runId));
        }

        const string insertRequest = """
            IF NOT EXISTS (SELECT 1 FROM dbo.ArchitectureRequests WHERE RequestId = @RequestId)
            INSERT INTO dbo.ArchitectureRequests
            (RequestId, SystemName, Environment, CloudProvider, RequestJson, CreatedUtc)
            VALUES (@RequestId, @SystemName, @Environment, @CloudProvider, @RequestJson, SYSUTCDATETIME());
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertRequest,
                new
                {
                    RequestId = requestId,
                    SystemName = "ContractSeed",
                    Environment = "prod",
                    CloudProvider = "Azure",
                    RequestJson = "{}",
                },
                cancellationToken: ct));

        const string insertRun = """
            IF NOT EXISTS (SELECT 1 FROM dbo.Runs WHERE RunId = @RunGuid)
            INSERT INTO dbo.Runs
            (RunId, ProjectId, Description, CreatedUtc, TenantId, WorkspaceId, ScopeProjectId, ArchitectureRequestId, LegacyRunStatus)
            VALUES (@RunGuid, N'ContractSeed', N'SQL contract test seed', SYSUTCDATETIME(), @TenantId, @WorkspaceId, @ScopeProjectId, @RequestId, N'Created');
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertRun,
                new
                {
                    RunGuid = runGuid,
                    RequestId = requestId,
                    TenantId = SeedTenantId,
                    WorkspaceId = SeedWorkspaceId,
                    ScopeProjectId = SeedScopeProjectId,
                },
                cancellationToken: ct));
    }

    /// <summary>Requires <see cref="InsertRequestAndRunAsync"/> for <paramref name="runId"/> first.</summary>
    public static async Task InsertAgentTaskAsync(
        IDbConnection connection,
        ArchLucid.Contracts.Agents.AgentTask task,
        CancellationToken ct)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.AgentTasks WHERE TaskId = @TaskId)
            INSERT INTO dbo.AgentTasks
            (TaskId, RunId, AgentType, Objective, Status, CreatedUtc, CompletedUtc, EvidenceBundleRef)
            VALUES (@TaskId, @RunId, @AgentType, @Objective, @Status, @CreatedUtc, @CompletedUtc, @EvidenceBundleRef);
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    task.TaskId,
                    task.RunId,
                    AgentType = task.AgentType.ToString(),
                    task.Objective,
                    Status = task.Status.ToString(),
                    task.CreatedUtc,
                    task.CompletedUtc,
                    task.EvidenceBundleRef,
                },
                cancellationToken: ct));
    }
}
