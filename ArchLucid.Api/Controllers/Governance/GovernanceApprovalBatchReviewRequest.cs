namespace ArchLucid.Api.Controllers;

/// <summary>Body for <c>POST /v1/governance/approval-requests/batch-review</c>.</summary>
public sealed class GovernanceApprovalBatchReviewRequest
{
    /// <summary>Approval request identifiers to process (max 50).</summary>
    public IReadOnlyList<string> ApprovalRequestIds { get; set; } = [];

    /// <summary><c>approve</c> or <c>reject</c> (case-insensitive).</summary>
    public string Decision { get; set; } = "";

    /// <summary>Optional comment recorded on each successful transition.</summary>
    public string? ReviewComment { get; set; }

    /// <summary>When empty, the current actor from the HTTP actor context is used.</summary>
    public string? ReviewedBy { get; set; }
}
