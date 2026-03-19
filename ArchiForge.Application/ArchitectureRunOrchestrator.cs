using ArchiForge.Application.Evidence;
using ArchiForge.AgentSimulator.Services;
using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using ArchiForge.DecisionEngine.Services;

namespace ArchiForge.Application;

public sealed class ArchitectureRunOrchestrator(
    ICoordinatorService coordinator,
    IAgentExecutor agentExecutor,
    IDecisionEngineService decisionEngine,
    IDecisionEngineV2 decisionEngineV2,
    IEvidenceBuilder evidenceBuilder)
{
    public async Task<DecisionMergeResult> ExecuteAsync(
        ArchitectureRequest request,
        CancellationToken cancellationToken = default)
    {
        var coordination = coordinator.CreateRun(request);

        if (!coordination.Success)
        {
            throw new InvalidOperationException(
                $"Coordination failed: {string.Join("; ", coordination.Errors)}");
        }

        var evidence = await evidenceBuilder.BuildAsync(
            coordination.Run.RunId,
            request,
            cancellationToken);

        var results = await agentExecutor.ExecuteAsync(
            coordination.Run.RunId,
            request,
            evidence,
            coordination.Tasks,
            cancellationToken);

        // v2 resolves structured decisions (weighted arguments). Evaluations are currently optional.
        var decisions = await decisionEngineV2.ResolveAsync(
            coordination.Run.RunId,
            request,
            evidence,
            coordination.Tasks,
            results,
            evaluations: [],
            cancellationToken: cancellationToken);

        var merged = decisionEngine.MergeResults(
            coordination.Run.RunId,
            request,
            "v1",
            results,
            evaluations: [],
            decisionNodes: decisions.ToList());

        merged.Decisions = decisions.ToList();
        return merged;
    }
}
