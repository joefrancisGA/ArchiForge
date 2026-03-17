using ArchiForge.Application.Evidence;
using ArchiForge.AgentSimulator.Services;
using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using ArchiForge.DecisionEngine.Services;

namespace ArchiForge.Application;

public sealed class ArchitectureRunOrchestrator
{
    private readonly ICoordinatorService _coordinator;
    private readonly IAgentExecutor _agentExecutor;
    private readonly IDecisionEngineService _decisionEngine;
    private readonly IDecisionEngineV2 _decisionEngineV2;
    private readonly IEvidenceBuilder _evidenceBuilder;

    public ArchitectureRunOrchestrator(
        ICoordinatorService coordinator,
        IAgentExecutor agentExecutor,
        IDecisionEngineService decisionEngine,
        IDecisionEngineV2 decisionEngineV2,
        IEvidenceBuilder evidenceBuilder)
    {
        _coordinator = coordinator;
        _agentExecutor = agentExecutor;
        _decisionEngine = decisionEngine;
        _decisionEngineV2 = decisionEngineV2;
        _evidenceBuilder = evidenceBuilder;
    }

    public async Task<DecisionMergeResult> ExecuteAsync(
        ArchitectureRequest request,
        CancellationToken cancellationToken = default)
    {
        var coordination = _coordinator.CreateRun(request);

        if (!coordination.Success)
        {
            throw new InvalidOperationException(
                $"Coordination failed: {string.Join("; ", coordination.Errors)}");
        }

        var evidence = await _evidenceBuilder.BuildAsync(
            coordination.Run.RunId,
            request,
            cancellationToken);

        var results = await _agentExecutor.ExecuteAsync(
            coordination.Run.RunId,
            request,
            evidence,
            coordination.Tasks,
            cancellationToken);

        // v2 resolves structured decisions (weighted arguments). Evaluations are currently optional.
        var decisions = await _decisionEngineV2.ResolveAsync(
            coordination.Run.RunId,
            request,
            evidence,
            coordination.Tasks,
            results,
            evaluations: [],
            cancellationToken: cancellationToken);

        var merged = _decisionEngine.MergeResults(
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
