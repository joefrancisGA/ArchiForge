using System.Data;
using System.Text.Json;

using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Decisions;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
///     Thread-safe in-memory <see cref="IDecisionNodeRepository" /> (JSON clone-on-read).
/// </summary>
public sealed class InMemoryDecisionNodeRepository : IDecisionNodeRepository
{
    private readonly Dictionary<string, List<DecisionNode>> _byRunId = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task CreateManyAsync(
        IReadOnlyCollection<DecisionNode> decisions,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(decisions);
        cancellationToken.ThrowIfCancellationRequested();

        if (decisions.Count == 0)
            return Task.CompletedTask;


        lock (_gate)

            foreach (DecisionNode decision in decisions)
            {
                if (!_byRunId.TryGetValue(decision.RunId, out List<DecisionNode>? list))
                {
                    list = [];
                    _byRunId[decision.RunId] = list;
                }

                list.Add(Clone(decision));
            }


        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DecisionNode>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_byRunId.TryGetValue(runId, out List<DecisionNode>? list))
                return Task.FromResult<IReadOnlyList<DecisionNode>>([]);


            List<DecisionNode> ordered = list
                .OrderBy(d => d.CreatedUtc)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<DecisionNode>>(ordered);
        }
    }

    private static DecisionNode Clone(DecisionNode source)
    {
        string json = JsonSerializer.Serialize(source, ContractJson.Default);
        DecisionNode? copy = JsonSerializer.Deserialize<DecisionNode>(json, ContractJson.Default);

        return copy ?? throw new InvalidOperationException("Clone produced null DecisionNode.");
    }
}
