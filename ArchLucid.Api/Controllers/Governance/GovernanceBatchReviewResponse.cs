namespace ArchLucid.Api.Controllers;

/// <summary>Per-item outcome for governance batch review.</summary>
public sealed class GovernanceBatchReviewItemResult
{
    public string ApprovalRequestId { get; set; } = "";

    public bool Succeeded { get; set; }

    /// <summary>Problem type or short code when <see cref="Succeeded"/> is false.</summary>
    public string? ErrorCode { get; set; }

    public string? Message { get; set; }
}

/// <summary>Response for <c>POST /v1/governance/approval-requests/batch-review</c>.</summary>
public sealed class GovernanceBatchReviewResponse
{
    public IReadOnlyList<GovernanceBatchReviewItemResult> Results { get; set; } = [];
}
