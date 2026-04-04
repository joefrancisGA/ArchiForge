using System.Text.Json;

using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Thread-safe in-memory <see cref="IDecisionTraceRepository"/> for architecture runs (JSON clone-on-read).
/// Distinct from the authority-layer decision trace contract in the Decisioning assembly.
/// </summary>
public sealed class InMemoryCoordinatorDecisionTraceRepository : IDecisionTraceRepository
{
    private readonly Dictionary<string, List<DecisionTrace>> _byRunId = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task CreateManyAsync(IEnumerable<DecisionTrace> traces, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(traces);
        cancellationToken.ThrowIfCancellationRequested();

        List<DecisionTrace> materialized = traces.ToList();

        lock (_gate)
        
            foreach (DecisionTrace trace in materialized)
            {
                if (!_byRunId.TryGetValue(trace.RunId, out List<DecisionTrace>? list))
                {
                    list = [];
                    _byRunId[trace.RunId] = list;
                }

                list.Add(Clone(trace));
            }
        

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DecisionTrace>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_byRunId.TryGetValue(runId, out List<DecisionTrace>? list))
            
                return Task.FromResult<IReadOnlyList<DecisionTrace>>([]);
            

            List<DecisionTrace> ordered = list
                .OrderBy(t => t.CreatedUtc)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<DecisionTrace>>(ordered);
        }
    }

    private static DecisionTrace Clone(DecisionTrace source)
    {
        string json = JsonSerializer.Serialize(source, ContractJson.Default);
        DecisionTrace? copy = JsonSerializer.Deserialize<DecisionTrace>(json, ContractJson.Default);

        return copy ?? throw new InvalidOperationException("Clone produced null DecisionTrace.");
    }
}
