using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.DecisionEngine.Services;

public interface IDecisionEngineService
{
    DecisionMergeResult MergeResults(
        ArchitectureRequest request,
        string manifestVersion,
        IReadOnlyCollection<AgentResult> results,
        string? parentManifestVersion = null);
}