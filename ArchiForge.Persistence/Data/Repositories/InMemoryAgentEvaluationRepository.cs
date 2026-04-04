using System.Text.Json;

using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Decisions;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Thread-safe in-memory <see cref="IAgentEvaluationRepository"/> (JSON clone-on-read).
/// Matches Dapper semantics: <see cref="CreateManyAsync"/> replaces all evaluations for the batch run id.
/// </summary>
public sealed class InMemoryAgentEvaluationRepository : IAgentEvaluationRepository
{
    private readonly Dictionary<string, List<AgentEvaluation>> _byRunId = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task CreateManyAsync(
        IReadOnlyCollection<AgentEvaluation> evaluations,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evaluations);
        cancellationToken.ThrowIfCancellationRequested();

        if (evaluations.Count == 0)
        
            return Task.CompletedTask;
        

        List<string> distinctRunIds = evaluations.Select(e => e.RunId).Distinct().ToList();
        if (distinctRunIds.Count > 1)
        
            throw new ArgumentException(
                $"All evaluations in a batch must belong to the same run. Found distinct RunIds: {string.Join(", ", distinctRunIds)}.",
                nameof(evaluations));
        

        string runId = evaluations.First().RunId;

        lock (_gate)
        
            _byRunId[runId] = evaluations.Select(Clone).ToList();
        

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AgentEvaluation>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_byRunId.TryGetValue(runId, out List<AgentEvaluation>? list))
            
                return Task.FromResult<IReadOnlyList<AgentEvaluation>>([]);
            

            List<AgentEvaluation> ordered = list
                .OrderBy(e => e.CreatedUtc)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<AgentEvaluation>>(ordered);
        }
    }

    private static AgentEvaluation Clone(AgentEvaluation source)
    {
        string json = JsonSerializer.Serialize(source, ContractJson.Default);
        AgentEvaluation? copy = JsonSerializer.Deserialize<AgentEvaluation>(json, ContractJson.Default);

        return copy ?? throw new InvalidOperationException("Clone produced null AgentEvaluation.");
    }
}
