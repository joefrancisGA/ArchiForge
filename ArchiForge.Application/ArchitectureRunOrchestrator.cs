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

    public ArchitectureRunOrchestrator(
        ICoordinatorService coordinator,
        IAgentExecutor agentExecutor,
        IDecisionEngineService decisionEngine)
    {
        _coordinator = coordinator;
        _agentExecutor = agentExecutor;
        _decisionEngine = decisionEngine;
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

        var results = await _agentExecutor.ExecuteAsync(
            coordination.Run.RunId,
            request,
            coordination.Tasks,
            cancellationToken);

        return _decisionEngine.MergeResults(
            coordination.Run.RunId,
            request,
            "v1",
            results);
    }
}
