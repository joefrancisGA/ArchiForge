using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Persistence.Data.Infrastructure;

using Dapper;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Dapper-backed persistence for <see cref="AgentTask"/> entities.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class AgentTaskRepository(IDbConnectionFactory connectionFactory) : IAgentTaskRepository
{
    public async Task CreateManyAsync(IEnumerable<AgentTask> tasks, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        const string sql = """
            INSERT INTO AgentTasks
            (
                TaskId,
                RunId,
                AgentType,
                Objective,
                Status,
                CreatedUtc,
                CompletedUtc,
                EvidenceBundleRef
            )
            VALUES
            (
                @TaskId,
                @RunId,
                @AgentType,
                @Objective,
                @Status,
                @CreatedUtc,
                @CompletedUtc,
                @EvidenceBundleRef
            );
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using IDbTransaction transaction = connection.BeginTransaction();

        IEnumerable<object> rows = tasks.Select(t => (object)new
        {
            t.TaskId,
            t.RunId,
            AgentType = t.AgentType.ToString(),
            t.Objective,
            Status = t.Status.ToString(),
            t.CreatedUtc,
            t.CompletedUtc,
            t.EvidenceBundleRef
        });

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            rows,
            transaction: transaction,
            cancellationToken: cancellationToken));

        transaction.Commit();
    }

    public async Task<IReadOnlyList<AgentTask>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        string sql = $"""
            SELECT
                TaskId,
                RunId,
                AgentType,
                Objective,
                Status,
                CreatedUtc,
                CompletedUtc,
                EvidenceBundleRef
            FROM AgentTasks
            WHERE RunId = @RunId
            ORDER BY CreatedUtc
            {SqlPagingSyntax.FirstRowsOnly(500)};
            """;

        IEnumerable<AgentTaskRow> rows = await connection.QueryAsync<AgentTaskRow>(new CommandDefinition(
            sql,
            new { RunId = runId },
            cancellationToken: cancellationToken));

        return [.. rows.Select(r => new AgentTask
        {
            TaskId = r.TaskId,
            RunId = r.RunId,
            AgentType = Enum.TryParse(r.AgentType, true, out AgentType agentType)
                ? agentType
                : throw new InvalidOperationException($"Unknown AgentType '{r.AgentType}' for task '{r.TaskId}'."),
            Objective = r.Objective,
            Status = Enum.TryParse(r.Status, true, out AgentTaskStatus status)
                ? status
                : throw new InvalidOperationException($"Unknown AgentTaskStatus '{r.Status}' for task '{r.TaskId}'."),
            CreatedUtc = r.CreatedUtc,
            CompletedUtc = r.CompletedUtc,
            EvidenceBundleRef = r.EvidenceBundleRef
        })];
    }

    private sealed class AgentTaskRow
    {
        public string TaskId { get; init; } = string.Empty;
        public string RunId { get; init; } = string.Empty;
        public string AgentType { get; init; } = string.Empty;
        public string Objective { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime CreatedUtc { get; init; }
        public DateTime? CompletedUtc { get; init; }
        public string? EvidenceBundleRef { get; init; }
    }
}
