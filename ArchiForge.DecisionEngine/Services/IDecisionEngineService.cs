using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.DecisionEngine.Services;

public interface IDecisionEngineService
{
    DecisionMergeResult MergeResults(
        string runId,
        ArchitectureRequest request,
        string manifestVersion,
        IReadOnlyCollection<AgentResult> results,
        IReadOnlyCollection<AgentEvaluation> evaluations,
        IReadOnlyCollection<DecisionNode> decisionNodes,
        string? parentManifestVersion = null);
}