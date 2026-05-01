using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Findings;
using ArchLucid.Contracts.Requests;

using EvalTypes = ArchLucid.Contracts.Decisions.EvaluationTypes;

namespace ArchLucid.Application.Decisions;

/// <summary>
///     Maps each <see cref="ArchitectureFinding" /> on agent results into
///     <see cref="AgentEvaluation" /> records so decision engines (e.g.
///     <c>ArchLucid.Decisioning.Merge.DecisionEngineV2</c>) can apply weighted
///     support/opposition. Deterministic; no LLM calls.
/// </summary>
/// <remarks>
///     Target task is the topology task when present so <c>TopologyAcceptance</c> scores
///     cross-agent signals; otherwise evaluations target the result task that held the finding.
/// </remarks>
public sealed class FindingsBackedAgentEvaluationService : IAgentEvaluationService
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

        AgentTask? topologyTask = tasks.FirstOrDefault(t => t.AgentType == AgentType.Topology);
        string? topologyTaskId = topologyTask?.TaskId;

        List<AgentEvaluation> evaluations = [];
        evaluations.AddRange(
            from result in results
            from finding in result.Findings
            let mapped = TryMapFinding(runId, result, finding, topologyTaskId)
            where mapped is not null
            select mapped);

        return Task.FromResult<IReadOnlyList<AgentEvaluation>>(evaluations);
    }

    private static AgentEvaluation? TryMapFinding(
        string runId,
        AgentResult result,
        ArchitectureFinding finding,
        string? topologyTaskId)
    {
        string targetTaskId = topologyTaskId ?? result.TaskId;

        return (result.AgentType, finding.Severity) switch
        {
            (AgentType.Compliance or AgentType.Critic, FindingSeverity.Critical) => Build(
                runId,
                targetTaskId,
                EvalTypes.Oppose,
                -0.30,
                finding),

            (AgentType.Compliance or AgentType.Critic, FindingSeverity.Error) => Build(
                runId,
                targetTaskId,
                EvalTypes.Oppose,
                -0.15,
                finding),

            (_, FindingSeverity.Warning) => Build(
                runId,
                targetTaskId,
                EvalTypes.Caution,
                -0.10,
                finding),

            (AgentType.Topology, FindingSeverity.Info) => Build(
                runId,
                targetTaskId,
                EvalTypes.Support,
                0.05,
                finding),

            (_, FindingSeverity.Info) => null,

            // Critical/Error from Topology/Cost: not translated to evaluations (see plan severity table).
            (_, FindingSeverity.Critical) => null,

            _ => null
        };
    }

    private static AgentEvaluation Build(
        string runId,
        string targetAgentTaskId,
        string evaluationType,
        double confidenceDelta,
        ArchitectureFinding finding)
    {
        List<string> evidenceRefs = [.. finding.EvidenceRefs];

        if (!string.IsNullOrWhiteSpace(finding.FindingId))
            evidenceRefs.Add($"finding:{finding.FindingId}");

        return new AgentEvaluation
        {
            RunId = runId,
            TargetAgentTaskId = targetAgentTaskId,
            EvaluationType = evaluationType,
            ConfidenceDelta = confidenceDelta,
            Rationale = string.IsNullOrWhiteSpace(finding.Message)
                ? $"{finding.Category} ({finding.Severity})"
                : finding.Message,
            EvidenceRefs = evidenceRefs
        };
    }
}
