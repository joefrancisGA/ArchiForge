namespace ArchiForge.Application.Runs;

/// <summary>
/// Optional HTTP idempotency for <see cref="IArchitectureRunService.CreateRunAsync"/> (scope + hashed key + request fingerprint).
/// </summary>
public sealed record CreateRunIdempotencyState(
    Guid TenantId,
    Guid WorkspaceId,
    Guid ProjectId,
    byte[] IdempotencyKeyHash,
    byte[] RequestFingerprint);
