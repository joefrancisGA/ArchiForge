using ArchLucid.Contracts.Requests;

namespace ArchLucid.Application.Runs.Orchestration;

/// <summary>
///     Coordinates and persists a new architecture run (create phase).
/// </summary>
public interface IArchitectureRunCreateOrchestrator
{
    Task<CreateRunResult> CreateRunAsync(
        ArchitectureRequest request,
        CreateRunIdempotencyState? idempotency = null,
        CancellationToken cancellationToken = default);
}
