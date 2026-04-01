using System.Data;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Tests.Support;

/// <summary>
/// Minimal <c>dbo.ArchitectureRequests</c> / <c>dbo.ArchitectureRuns</c> / <c>dbo.AgentTasks</c> rows for Data-layer SQL contract tests (FK chain).
/// </summary>
public static class ArchitectureCommitTestSeed
{
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

    /// <summary>Inserts request + run (idempotent on <paramref name="requestId"/> / <paramref name="runId"/>).</summary>
    public static async Task InsertRequestAndRunAsync(
        SqlConnection connection,
        string requestId,
        string runId,
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
                    SystemName = "ContractSeed",
                    Environment = "prod",
                    CloudProvider = "Azure",
                    RequestJson = "{}",
                },
                cancellationToken: ct));

        const string insertRun = """
            IF NOT EXISTS (SELECT 1 FROM dbo.ArchitectureRuns WHERE RunId = @RunId)
            INSERT INTO dbo.ArchitectureRuns
            (RunId, RequestId, Status, CreatedUtc, CompletedUtc, CurrentManifestVersion, ContextSnapshotId, GraphSnapshotId, ArtifactBundleId)
            VALUES (@RunId, @RequestId, @Status, SYSUTCDATETIME(), NULL, NULL, NULL, NULL, NULL);
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertRun,
                new
                {
                    RunId = runId,
                    RequestId = requestId,
                    Status = "Created",
                },
                cancellationToken: ct));
    }

    /// <summary>Requires <see cref="InsertRequestAndRunAsync"/> for <paramref name="runId"/> first.</summary>
    public static async Task InsertAgentTaskAsync(
        IDbConnection connection,
        ArchiForge.Contracts.Agents.AgentTask task,
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
