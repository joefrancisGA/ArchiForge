using System.Text.Json;

using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Thread-safe in-memory <see cref="ICoordinatorDecisionTraceRepository"/> for architecture runs (JSON clone-on-read).
/// Distinct from the authority-layer decision trace contract in the Decisioning assembly.
/// </summary>
public sealed class InMemoryCoordinatorDecisionTraceRepository : ICoordinatorDecisionTraceRepository
{
    private readonly Dictionary<string, List<RunEventTrace>> _byRunId = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task CreateManyAsync(IEnumerable<RunEventTrace> traces, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(traces);
        cancellationToken.ThrowIfCancellationRequested();

        List<RunEventTrace> materialized = traces.ToList();

        lock (_gate)
        
            foreach (RunEventTrace trace in materialized)
            {
                if (!_byRunId.TryGetValue(trace.RunId, out List<RunEventTrace>? list))
                {
                    list = [];
                    _byRunId[trace.RunId] = list;
                }

                list.Add(Clone(trace));
            }
        

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RunEventTrace>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_byRunId.TryGetValue(runId, out List<RunEventTrace>? list))
            
                return Task.FromResult<IReadOnlyList<RunEventTrace>>([]);
            

            List<RunEventTrace> ordered = list
                .OrderBy(t => t.CreatedUtc)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<RunEventTrace>>(ordered);
        }
    }

    private static RunEventTrace Clone(RunEventTrace source)
    {
        string json = JsonSerializer.Serialize(source, ContractJson.Default);
        RunEventTrace? copy = JsonSerializer.Deserialize<RunEventTrace>(json, ContractJson.Default);

        return copy ?? throw new InvalidOperationException("Clone produced null RunEventTrace.");
    }
}
