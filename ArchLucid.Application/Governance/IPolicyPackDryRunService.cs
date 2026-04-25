using ArchLucid.Contracts.Governance;

namespace ArchLucid.Application.Governance;

/// <summary>
///     Application service for governance "what-if" dry-runs: evaluate a proposed set of policy thresholds
///     against a fixed list of run ids and return per-run "would have blocked" deltas <em>without</em>
///     modifying any governance state.
/// </summary>
/// <remarks>
///     <para>
///         The proposed-thresholds payload <strong>must</strong> flow through the existing LLM-prompt
///         redaction pipeline before serialisation to <c>dbo.AuditEvents</c>
///         (PENDING_QUESTIONS Q37). The audit event type is
///         <c>AuditEventTypes.GovernanceDryRunRequested</c>.
///     </para>
///     <para>
///         Pagination defaults to <see cref="DefaultPageSize" /> and is server-clamped to
///         <see cref="MaxPageSize" /> per page (PENDING_QUESTIONS Q38).
///     </para>
/// </remarks>
public interface IPolicyPackDryRunService
{
    /// <summary>Default page size when the caller omits <c>pageSize</c> (PENDING_QUESTIONS Q38).</summary>
    public const int DefaultPageSize = 20;

    /// <summary>Server-side maximum page size (PENDING_QUESTIONS Q38).</summary>
    public const int MaxPageSize = 100;

    /// <summary>Hard upper bound on the number of run ids the caller may dry-run in a single request.</summary>
    public const int MaxEvaluatedRuns = 500;

    /// <summary>
    ///     Computes per-run dry-run deltas, redacts the proposed thresholds, persists the audit row, and
    ///     returns the (paged) response.
    /// </summary>
    Task<PolicyPackDryRunResponse> EvaluateAsync(
        Guid policyPackId,
        IReadOnlyDictionary<string, string> proposedThresholds,
        IReadOnlyList<string> evaluateAgainstRunIds,
        int? pageSize,
        int? page,
        CancellationToken cancellationToken = default);
}
