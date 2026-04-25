using ArchLucid.Decisioning.Governance.PolicyPacks;

namespace ArchLucid.Decisioning.Governance.Resolution;

/// <summary>
///     Complete output of <see cref="IEffectiveGovernanceResolver.ResolveAsync" />: merged content, per-item decisions,
///     conflicts, and summary notes.
/// </summary>
/// <remarks>
///     Serialized to JSON by <c>ArchLucid.Api.Controllers.GovernanceResolutionController</c> for operator inspection.
///     Only <see cref="EffectiveContent" /> is consumed by <see cref="EffectiveGovernanceLoader" />.
/// </remarks>
public class EffectiveGovernanceResolutionResult
{
    /// <summary>Scope dimension echoed from the request / ambient context.</summary>
    public Guid TenantId
    {
        get;
        set;
    }

    /// <summary>Scope dimension echoed from the request / ambient context.</summary>
    public Guid WorkspaceId
    {
        get;
        set;
    }

    /// <summary>Scope dimension echoed from the request / ambient context.</summary>
    public Guid ProjectId
    {
        get;
        set;
    }

    /// <summary>
    ///     Final merged <see cref="PolicyPackContentDocument" /> after precedence rules (single source of truth for runtime
    ///     filters).
    /// </summary>
    public PolicyPackContentDocument EffectiveContent
    {
        get;
        set;
    } = new();

    /// <summary>One entry per resolved item (rule id, key, or dictionary key) explaining the winner.</summary>
    public List<GovernanceResolutionDecision> Decisions
    {
        get;
        set;
    } = [];

    /// <summary>Subset of situations where multiple packs competed (duplicate id/key or divergent dictionary values).</summary>
    public List<GovernanceConflictRecord> Conflicts
    {
        get;
        set;
    } = [];

    /// <summary>Human-readable summary lines (counts and high-level stats).</summary>
    public List<string> Notes
    {
        get;
        set;
    } = [];
}
