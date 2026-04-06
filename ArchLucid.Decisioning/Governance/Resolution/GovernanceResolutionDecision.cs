using ArchiForge.Decisioning.Governance.PolicyPacks;

namespace ArchiForge.Decisioning.Governance.Resolution;

/// <summary>
/// Explainability record: which policy pack version won for a single logical item (e.g. one alert rule id or metadata key).
/// </summary>
/// <remarks>
/// Populated by <see cref="EffectiveGovernanceResolver"/> for every distinct item key after merge. The first candidate in
/// <see cref="Candidates"/> after ordering is the winner (<see cref="GovernanceResolutionCandidate.WasSelected"/> set to <c>true</c>).
/// </remarks>
public class GovernanceResolutionDecision
{
    /// <summary>Facet name, e.g. <c>AlertRule</c>, <c>Metadata</c>, <c>ComplianceRuleKey</c>.</summary>
    public string ItemType { get; set; } = null!;

    /// <summary>Key within the facet: GUID string, dictionary key, or compliance key string.</summary>
    public string ItemKey { get; set; } = null!;

    /// <summary><see cref="PolicyPack.PolicyPackId"/> of the winning pack.</summary>
    public Guid WinningPolicyPackId { get; set; }

    /// <summary>Display name of the winning pack (operator UX).</summary>
    public string WinningPolicyPackName { get; set; } = null!;

    /// <summary>Winning published version label.</summary>
    public string WinningVersion { get; set; } = null!;

    /// <summary><see cref="GovernanceScopeLevel"/> of the winning assignment.</summary>
    public string WinningScopeLevel { get; set; } = null!;

    /// <summary>Natural language justification (tie-breakers, precedence).</summary>
    public string ResolutionReason { get; set; } = null!;

    /// <summary>All competing contributions, ordered by precedence then recency.</summary>
    public List<GovernanceResolutionCandidate> Candidates { get; set; } = [];
}
