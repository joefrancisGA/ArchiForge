namespace ArchLucid.Application.Governance;

/// <summary>
///     Thrown when a governance reviewer attempts to approve or reject an approval request they submitted (segregation of
///     duties).
/// </summary>
public sealed class GovernanceSelfApprovalException : InvalidOperationException
{
    public GovernanceSelfApprovalException(string approvalRequestId, string actor)
        : base(BuildMessage(approvalRequestId, actor))
    {
        ApprovalRequestId = approvalRequestId;
        Actor = actor;
    }

    /// <summary>Identifier of the governance approval request.</summary>
    public string ApprovalRequestId
    {
        get;
    }

    /// <summary>Identity that attempted the review (same as submitter).</summary>
    public string Actor
    {
        get;
    }

    private static string BuildMessage(string approvalRequestId, string actor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvalRequestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);

        return
            $"Segregation of duties violation: the reviewer '{actor}' cannot approve or reject their own request '{approvalRequestId}'. A different reviewer is required.";
    }
}
