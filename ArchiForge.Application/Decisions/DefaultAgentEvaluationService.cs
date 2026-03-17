using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.Application.Decisions;

/// <summary>
/// Minimal deterministic evaluator. First cut returns no evaluations; decisions can still be resolved from results.
/// </summary>
public sealed class DefaultAgentEvaluationService : IAgentEvaluationService
{
    public Task<IReadOnlyList<AgentEvaluation>> EvaluateAsync(
        string runId,
        ArchitectureRequest request,
        AgentEvidencePackage evidence,
        IReadOnlyCollection<AgentTask> tasks,
        IReadOnlyCollection<AgentResult> results,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(results);

        return Task.FromResult<IReadOnlyList<AgentEvaluation>>([]);
    }
}

