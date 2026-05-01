namespace ArchLucid.Application.Runs;

/// <summary>
///     Optional HTTP idempotency for create-run (via
///     <see cref="Orchestration.IArchitectureRunCreateOrchestrator.CreateRunAsync" />; scope + hashed key + request
///     fingerprint).
/// </summary>
public sealed record CreateRunIdempotencyState(
    Guid TenantId,
    Guid WorkspaceId,
    Guid ProjectId,
    byte[] IdempotencyKeyHash,
    byte[] RequestFingerprint);
