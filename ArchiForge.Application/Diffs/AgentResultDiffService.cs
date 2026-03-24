using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;

namespace ArchiForge.Application.Diffs;

/// <summary>
/// Compares two sets of <see cref="AgentResult"/> objects (one per run) and produces a per-agent-type diff
/// covering claims, findings, evidence references, required controls, and warnings.
/// </summary>
public sealed class AgentResultDiffService : IAgentResultDiffService
{
    /// <summary>
    /// Produces an <see cref="AgentResultDiffResult"/> describing the differences between the latest
    /// result for each agent type across the two runs.
    /// </summary>
    public AgentResultDiffResult Compare(
        string leftRunId,
        IReadOnlyCollection<AgentResult> leftResults,
        string rightRunId,
        IReadOnlyCollection<AgentResult> rightResults)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leftRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(rightRunId);
        ArgumentNullException.ThrowIfNull(leftResults);
        ArgumentNullException.ThrowIfNull(rightResults);

        var result = new AgentResultDiffResult
        {
            LeftRunId = leftRunId,
            RightRunId = rightRunId
        };

        var allAgentTypes = leftResults.Select(r => r.AgentType)
            .Union(rightResults.Select(r => r.AgentType))
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        foreach (var agentType in allAgentTypes)
        {
            var left = leftResults
                .Where(r => r.AgentType == agentType)
                .OrderByDescending(r => r.CreatedUtc)
                .FirstOrDefault();

            var right = rightResults
                .Where(r => r.AgentType == agentType)
                .OrderByDescending(r => r.CreatedUtc)
                .FirstOrDefault();

            result.AgentDeltas.Add(BuildDelta(agentType, left, right));
        }

        if (result.AgentDeltas.Count == 0)
        {
            result.Warnings.Add("No agent results were available to compare.");
        }

        return result;
    }

    /// <summary>
    /// Builds the per-agent-type delta by diffing claims, evidence refs, findings, required controls, and warnings.
    /// </summary>
    private static AgentResultDelta BuildDelta(
        AgentType agentType,
        AgentResult? left,
        AgentResult? right)
    {
        var delta = new AgentResultDelta
        {
            AgentType = agentType,
            LeftExists = left is not null,
            RightExists = right is not null,
            LeftConfidence = left?.Confidence,
            RightConfidence = right?.Confidence
        };

        var leftClaims = left?.Claims ?? [];
        var rightClaims = right?.Claims ?? [];

        var leftEvidence = left?.EvidenceRefs ?? [];
        var rightEvidence = right?.EvidenceRefs ?? [];

        var leftFindings = left?.Findings?.Where(f => f is not null).Select(f => f.Message).Where(m => !string.IsNullOrWhiteSpace(m)).ToList() ?? [];
        var rightFindings = right?.Findings?.Where(f => f is not null).Select(f => f.Message).Where(m => !string.IsNullOrWhiteSpace(m)).ToList() ?? [];

        var leftControls = left?.ProposedChanges?.RequiredControls ?? [];
        var rightControls = right?.ProposedChanges?.RequiredControls ?? [];

        var leftWarnings = left?.ProposedChanges?.Warnings ?? [];
        var rightWarnings = right?.ProposedChanges?.Warnings ?? [];

        delta.AddedClaims = Except(rightClaims, leftClaims);
        delta.RemovedClaims = Except(leftClaims, rightClaims);

        delta.AddedEvidenceRefs = Except(rightEvidence, leftEvidence);
        delta.RemovedEvidenceRefs = Except(leftEvidence, rightEvidence);

        delta.AddedFindings = Except(rightFindings, leftFindings);
        delta.RemovedFindings = Except(leftFindings, rightFindings);

        delta.AddedRequiredControls = Except(rightControls, leftControls);
        delta.RemovedRequiredControls = Except(leftControls, rightControls);

        delta.AddedWarnings = Except(rightWarnings, leftWarnings);
        delta.RemovedWarnings = Except(leftWarnings, rightWarnings);

        return delta;
    }

    private static List<string> Except(
        IReadOnlyCollection<string> left,
        IReadOnlyCollection<string> right)
    {
        var rightSet = right.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return left
            .Where(x => !rightSet.Contains(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }
}
