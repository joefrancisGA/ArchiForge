using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Diagnostics;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Decisions;

/// <summary>
/// Minimal deterministic evaluator used in dev/test environments.
/// Returns no evaluations; decision scoring relies entirely on agent result confidence.
/// Register a real <see cref="IAgentEvaluationService"/> in production to enable
/// critic-signal weighting.
/// </summary>
public sealed class DefaultAgentEvaluationService(ILogger<DefaultAgentEvaluationService> logger)
    : IAgentEvaluationService
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
        cancellationToken.ThrowIfCancellationRequested();

        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning(
                "DefaultAgentEvaluationService is active for run '{RunId}'. " +
                "No evaluations will be produced; critic-signal weighting is disabled. " +
                "Register a real IAgentEvaluationService for production use.",
                LogSanitizer.Sanitize(runId));
        }

        return Task.FromResult<IReadOnlyList<AgentEvaluation>>([]);
    }
}
