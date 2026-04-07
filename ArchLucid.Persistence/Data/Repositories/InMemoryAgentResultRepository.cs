using System.Data;
using System.Text.Json;

using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
/// Thread-safe in-memory <see cref="IAgentResultRepository"/> for tests (clone-on-read for isolation).
/// </summary>
public sealed class InMemoryAgentResultRepository : IAgentResultRepository
{
    private readonly List<AgentResult> _results = [];
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task CreateAsync(
        AgentResult result,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _results.RemoveAll(r =>
                string.Equals(r.RunId, result.RunId, StringComparison.Ordinal) &&
                string.Equals(r.TaskId, result.TaskId, StringComparison.Ordinal));
            _results.Add(Clone(result));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CreateManyAsync(
        IReadOnlyList<AgentResult> results,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(results);
        cancellationToken.ThrowIfCancellationRequested();
        if (results.Count == 0)
        
            return Task.CompletedTask;
        

        List<string> distinctRunIds = results.Select(r => r.RunId).Distinct().ToList();
        if (distinctRunIds.Count > 1)
        
            throw new ArgumentException(
                $"All results in a batch must belong to the same run. Found distinct RunIds: {string.Join(", ", distinctRunIds)}.",
                nameof(results));
        

        string runId = distinctRunIds[0];
        lock (_gate)
        {
            _results.RemoveAll(r => string.Equals(r.RunId, runId, StringComparison.Ordinal));
            foreach (AgentResult r in results)
            
                _results.Add(Clone(r));
            
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AgentResult>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            List<AgentResult> list = _results
                .Where(r => string.Equals(r.RunId, runId, StringComparison.Ordinal))
                .OrderBy(r => r.CreatedUtc)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<AgentResult>>(list);
        }
    }

    private static AgentResult Clone(AgentResult source)
    {
        string json = JsonSerializer.Serialize(source, ContractJson.Default);
        AgentResult? copy = JsonSerializer.Deserialize<AgentResult>(json, ContractJson.Default);

        return copy ?? throw new InvalidOperationException("Clone produced null AgentResult.");
    }
}
