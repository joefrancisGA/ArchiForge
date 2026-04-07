using System.Text.Json;

using ArchLucid.Contracts.Common;
using System.Data;

using ArchLucid.Contracts.DecisionTraces;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
/// Thread-safe in-memory <see cref="ICoordinatorDecisionTraceRepository"/> for architecture runs (JSON clone-on-read).
/// Distinct from the authority-layer decision trace contract in the Decisioning assembly.
/// </summary>
public sealed class InMemoryCoordinatorDecisionTraceRepository : ICoordinatorDecisionTraceRepository
{
    private readonly Dictionary<string, List<DecisionTrace>> _byRunId = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task CreateManyAsync(
        IEnumerable<DecisionTrace> traces,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(traces);
        cancellationToken.ThrowIfCancellationRequested();

        List<DecisionTrace> materialized = traces.ToList();

        lock (_gate)

            foreach (DecisionTrace trace in materialized)
            {
                RunEventTracePayload run = trace.RequireRunEvent();

                if (!_byRunId.TryGetValue(run.RunId, out List<DecisionTrace>? list))
                {
                    list = [];
                    _byRunId[run.RunId] = list;
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
                .OrderBy(t => t.RequireRunEvent().CreatedUtc)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<DecisionTrace>>(ordered);
        }
    }

    private static DecisionTrace Clone(DecisionTrace source)
    {
        RunEventTracePayload payload = source.RequireRunEvent();
        string json = JsonSerializer.Serialize(payload, ContractJson.Default);
        RunEventTracePayload? copy = JsonSerializer.Deserialize<RunEventTracePayload>(json, ContractJson.Default);

        return copy is null
            ? throw new InvalidOperationException("Clone produced null RunEventTracePayload.")
            : RunEventTrace.From(copy);
    }
}
