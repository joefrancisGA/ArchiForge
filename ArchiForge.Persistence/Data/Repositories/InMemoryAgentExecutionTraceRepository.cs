using System.Text.Json;

using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Thread-safe in-memory <see cref="IAgentExecutionTraceRepository"/> for tests (JSON clone-on-read).
/// </summary>
public sealed class InMemoryAgentExecutionTraceRepository : IAgentExecutionTraceRepository
{
    private readonly List<AgentExecutionTrace> _items = [];
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task CreateAsync(AgentExecutionTrace trace, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trace);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        
            _items.Add(Clone(trace));
        

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AgentExecutionTrace>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            List<AgentExecutionTrace> list = _items
                .Where(t => string.Equals(t.RunId, runId, StringComparison.Ordinal))
                .OrderBy(t => t.CreatedUtc)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<AgentExecutionTrace>>(list);
        }
    }

    /// <inheritdoc />
    public Task<(IReadOnlyList<AgentExecutionTrace> Traces, int TotalCount)> GetPagedByRunIdAsync(
        string runId,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            List<AgentExecutionTrace> ordered = _items
                .Where(t => string.Equals(t.RunId, runId, StringComparison.Ordinal))
                .OrderBy(t => t.CreatedUtc)
                .ToList();

            int total = ordered.Count;
            int clampedOffset = Math.Max(0, offset);
            int clampedLimit = Math.Clamp(limit, 1, 500);
            List<AgentExecutionTrace> page = ordered
                .Skip(clampedOffset)
                .Take(clampedLimit)
                .Select(Clone)
                .ToList();

            return Task.FromResult<(IReadOnlyList<AgentExecutionTrace>, int)>((page, total));
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AgentExecutionTrace>> GetByTaskIdAsync(string taskId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            List<AgentExecutionTrace> list = _items
                .Where(t => string.Equals(t.TaskId, taskId, StringComparison.Ordinal))
                .OrderBy(t => t.CreatedUtc)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<AgentExecutionTrace>>(list);
        }
    }

    private static AgentExecutionTrace Clone(AgentExecutionTrace source)
    {
        string json = JsonSerializer.Serialize(source, ContractJson.Default);
        AgentExecutionTrace? copy = JsonSerializer.Deserialize<AgentExecutionTrace>(json, ContractJson.Default);

        return copy ?? throw new InvalidOperationException("Clone produced null AgentExecutionTrace.");
    }
}
