using ArchiForge.Contracts.Requests;

namespace ArchiForge.Coordinator.Services;

/// <summary>
/// Maps an <see cref="ArchitectureRequest"/> into an authority run via <see cref="ArchiForge.Persistence.Orchestration.IAuthorityRunOrchestrator"/>, then materializes coordinator contracts (run, evidence bundle, starter agent tasks).
/// </summary>
/// <remarks>
/// Implementation: <see cref="CoordinatorService"/>. Primary consumer: <c>ArchiForge.Application.ArchitectureRunService</c> (registered scoped in API).
/// </remarks>
public interface ICoordinatorService
{
    /// <summary>
    /// Validates required fields, executes the persistence orchestrator, and builds topology/cost/compliance/critic starter tasks plus metadata-only <see cref="KnowledgeGraph.Models.GraphSnapshot"/> shell for downstream agents.
    /// </summary>
    /// <param name="request">Inbound architecture request (system name, description, constraints, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="CoordinationResult"/> with <see cref="CoordinationResult.Errors"/> populated when validation fails (no orchestrator call).</returns>
    Task<CoordinationResult> CreateRunAsync(
        ArchitectureRequest request,
        CancellationToken cancellationToken = default);
}
