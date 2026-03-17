using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.Application.Decisions;

public interface IAgentEvaluationService
{
    Task<IReadOnlyList<AgentEvaluation>> EvaluateAsync(
        string runId,
        ArchitectureRequest request,
        AgentEvidencePackage evidence,
        IReadOnlyCollection<AgentTask> tasks,
        IReadOnlyCollection<AgentResult> results,
        CancellationToken cancellationToken = default);
}

