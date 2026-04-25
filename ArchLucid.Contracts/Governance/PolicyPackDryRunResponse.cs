namespace ArchLucid.Contracts.Governance;

/// <summary>
///     Response for <c>POST /v1/governance/policy-packs/{id}/dry-run</c>: per-run delta showing what the
///     proposed thresholds <em>would</em> have done if they had been live, plus the redacted audit record
///     of the proposed thresholds (so reviewers see the exact value persisted to <c>dbo.AuditEvents</c>).
/// </summary>
/// <remarks>
///     Read-only by construction — no run state, governance state, or commit happens. The audit row is
///     persisted with <c>EventType = AuditEventTypes.GovernanceDryRunRequested</c> and the redacted
///     thresholds payload (PENDING_QUESTIONS Q37). Pagination defaults to <c>20</c> and is server-clamped
///     to <c>100</c> per page (PENDING_QUESTIONS Q38).
/// </remarks>
public sealed class PolicyPackDryRunResponse
{
    /// <summary>The policy pack the dry-run was evaluated against (route id).</summary>
    public Guid PolicyPackId
    {
        get; init;
    }

    /// <summary>UTC timestamp when the dry-run was evaluated.</summary>
    public DateTime EvaluatedUtc
    {
        get; init;
    }

    /// <summary>1-based page index actually returned (clamped to a valid range).</summary>
    public int Page
    {
        get; init;
    }

    /// <summary>Page size actually returned (default 20, server-clamped to 1..100).</summary>
    public int PageSize
    {
        get; init;
    }

    /// <summary>Total number of run ids the caller asked the service to evaluate.</summary>
    public int TotalRequestedRuns
    {
        get; init;
    }

    /// <summary>Number of items in <see cref="Items" /> on this page.</summary>
    public int ReturnedRuns
    {
        get; init;
    }

    /// <summary>
    ///     The proposed-thresholds JSON <em>after</em> the LLM-prompt redaction pipeline has scrubbed it.
    ///     This is the same string persisted to the audit row's <c>DataJson.proposedThresholdsRedacted</c>
    ///     field so reviewers can see what was simulated without leaking PII.
    /// </summary>
    public string ProposedThresholdsRedactedJson
    {
        get; init;
    } = "{}";

    /// <summary>
    ///     Tally across <em>all</em> evaluated run ids in the request (not just the current page),
    ///     so a UI can render a stable "would have blocked N of M" headline regardless of pagination.
    /// </summary>
    public PolicyPackDryRunDeltaCounts DeltaCounts
    {
        get; init;
    } = new();

    /// <summary>Per-run dry-run results for the current page (newest first when input order is preserved).</summary>
    public IReadOnlyList<PolicyPackDryRunRunItem> Items
    {
        get; init;
    } = [];
}
