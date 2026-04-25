using System.Diagnostics.CodeAnalysis;

namespace ArchLucid.Api.Models;

/// <summary>
///     Request body for <c>POST /v1/governance/policy-packs/{id}/dry-run</c> — proposed threshold overrides
///     and the run ids to evaluate them against. Read-auth gated; no real commit happens.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "API request DTO; no business logic.")]
public sealed class PolicyPackDryRunRequest
{
    /// <summary>
    ///     Threshold key → proposed value (transported as <c>string</c> so JSON shape is stable as new
    ///     threshold types are added; service parses to <see cref="double" /> and ignores unknown keys).
    ///     Values flow through the LLM-prompt redaction pipeline before audit serialisation
    ///     (<see cref="ArchLucid.Core.Audit.AuditEventTypes.GovernanceDryRunRequested" />, PENDING_QUESTIONS Q37).
    /// </summary>
    public Dictionary<string, string> ProposedThresholds
    {
        get;
        set;
    } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The run ids the caller wants to dry-run the proposed thresholds against.</summary>
    public List<string> EvaluateAgainstRunIds
    {
        get;
        set;
    } = [];
}
