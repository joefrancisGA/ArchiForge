namespace ArchLucid.Application.Governance;

/// <summary>
///     Thrown when approve/reject lost a race: another request already transitioned the approval request out of a
///     reviewable state.
/// </summary>
public sealed class GovernanceApprovalReviewConflictException : InvalidOperationException
{
    public GovernanceApprovalReviewConflictException(string approvalRequestId, string attemptedOutcome,
        string currentStatus)
        : base(BuildMessage(approvalRequestId, attemptedOutcome, currentStatus))
    {
        ApprovalRequestId = approvalRequestId;
        AttemptedOutcome = attemptedOutcome;
        CurrentStatus = currentStatus;
    }

    public string ApprovalRequestId
    {
        get;
    }

    public string AttemptedOutcome
    {
        get;
    }

    public string CurrentStatus
    {
        get;
    }

    private static string BuildMessage(string approvalRequestId, string attemptedOutcome, string currentStatus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvalRequestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(attemptedOutcome);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentStatus);

        return
            $"Governance approval request '{approvalRequestId}' is no longer reviewable (current status '{currentStatus}'). " +
            $"Another request may have completed a concurrent {attemptedOutcome}.";
    }
}
