namespace ArchLucid.Contracts.Findings;

/// <summary>Action recorded in a durable finding review trail.</summary>
public enum FindingReviewAction
{
    Approve = 0,
    Reject = 1,
    Override = 2,
    Escalate = 3
}
