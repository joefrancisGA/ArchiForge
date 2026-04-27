using ArchLucid.Contracts.Requests;

namespace ArchLucid.Application.Runs.Coordination;

/// <summary>
///     Maps an <see cref="ArchitectureRequest" /> into an authority run via
///     <see cref="ArchLucid.Persistence.Orchestration.IAuthorityRunOrchestrator" />, then materializes
///     <see cref="CoordinationResult" /> (run, evidence bundle, starter agent tasks).
/// </summary>
/// <remarks>
///     Implementation: <see cref="ArchitectureRunAuthorityCoordination" />. Primary consumer:
///     <see cref="Orchestration.ArchitectureRunCreateOrchestrator" /> (scoped in API/worker composition).
/// </remarks>
public interface IArchitectureRunAuthorityCoordination
{
    /// <summary>
    ///     Validates required fields, executes the persistence orchestrator, and builds topology/cost/compliance/critic
    ///     starter tasks plus metadata-only shell for downstream agents.
    /// </summary>
    /// <param name="request">Inbound architecture request (system name, description, constraints, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     <see cref="CoordinationResult" /> with <see cref="CoordinationResult.Errors" /> populated when validation
    ///     fails (no orchestrator call).
    /// </returns>
    Task<CoordinationResult> CreateRunAsync(
        ArchitectureRequest request,
        CancellationToken cancellationToken = default);
}
