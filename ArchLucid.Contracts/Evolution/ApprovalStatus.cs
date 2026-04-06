namespace ArchiForge.Contracts.Evolution;

/// <summary>Human-gate state for a 60R candidate change set (simulation does not imply approval).</summary>
public enum ApprovalStatus
{
    Unspecified = 0,

    PendingReview,

    Approved,

    Rejected,
}
