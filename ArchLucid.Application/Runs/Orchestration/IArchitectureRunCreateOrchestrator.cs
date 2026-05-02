using ArchLucid.Contracts.Requests;

namespace ArchLucid.Application.Runs.Orchestration;

/// <summary>
///     Coordinates and persists a new architecture run (create phase).
/// </summary>
/// <remarks>
///     <see cref="CreateRunAsync" /> evaluates <see cref="IRequestContentSafetyPrecheck" /> on the submitted request
///     before authority coordination or persistence (aligned with execute and file-import paths).
/// </remarks>
public interface IArchitectureRunCreateOrchestrator
{
    Task<CreateRunResult> CreateRunAsync(
        ArchitectureRequest request,
        CreateRunIdempotencyState? idempotency = null,
        CancellationToken cancellationToken = default);
}
