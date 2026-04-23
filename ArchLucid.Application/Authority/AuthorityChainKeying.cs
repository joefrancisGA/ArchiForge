namespace ArchLucid.Application.Authority;

/// <summary>Caller-supplied primary keys for the authority manifest persistence chain.</summary>
public sealed record AuthorityChainKeying(
    Guid ManifestId,
    Guid ContextSnapshotId,
    Guid GraphSnapshotId,
    Guid FindingsSnapshotId,
    Guid DecisionTraceId);
