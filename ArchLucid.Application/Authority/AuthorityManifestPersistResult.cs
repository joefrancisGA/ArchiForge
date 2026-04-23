namespace ArchLucid.Application.Authority;

/// <summary>Primary keys produced when persisting the authority snapshot + decision trace + golden manifest chain.</summary>
public sealed record AuthorityManifestPersistResult(
    Guid ContextSnapshotId,
    Guid GraphSnapshotId,
    Guid FindingsSnapshotId,
    Guid DecisionTraceId,
    Guid GoldenManifestId);
