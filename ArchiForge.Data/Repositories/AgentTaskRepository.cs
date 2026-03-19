using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Data.Infrastructure;
using Dapper;

namespace ArchiForge.Data.Repositories;

public sealed class AgentTaskRepository(IDbConnectionFactory connectionFactory) : IAgentTaskRepository
{
    public async Task CreateManyAsync(IEnumerable<AgentTask> tasks, CancellationToken cancellationToken = default)
    {
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

        using var connection = connectionFactory.CreateConnection();

        var rows = tasks.Select(t => new
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
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<AgentTask>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default)
    {
        const string sql = """
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
            ORDER BY CreatedUtc;
            """;

        using var connection = connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<AgentTaskRow>(new CommandDefinition(
            sql,
            new { RunId = runId },
            cancellationToken: cancellationToken));

        return [.. rows.Select(r => new AgentTask { TaskId = r.TaskId, RunId = r.RunId, AgentType = Enum.TryParse<AgentType>(r.AgentType, true, out var agentType) ? agentType : AgentType.Topology, Objective = r.Objective, Status = Enum.TryParse<AgentTaskStatus>(r.Status, true, out var status) ? status : AgentTaskStatus.Created, CreatedUtc = r.CreatedUtc, CompletedUtc = r.CompletedUtc, EvidenceBundleRef = r.EvidenceBundleRef })];
    }

    private sealed class AgentTaskRow
    {
        public string TaskId { get; set; } = string.Empty;
        public string RunId { get; set; } = string.Empty;
        public string AgentType { get; set; } = string.Empty;
        public string Objective { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; }
        public DateTime? CompletedUtc { get; set; }
        public string? EvidenceBundleRef { get; set; }
    }
}