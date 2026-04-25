namespace ArchLucid.Decisioning.Governance.Resolution;

/// <summary>
///     One pack’s contribution to a single merge item (candidate in a precedence contest).
/// </summary>
/// <remarks>
///     Built by <see cref="EffectiveGovernanceResolver" />; surfaced in API JSON for transparency.
///     <see cref="ValueJson" /> meaning depends on
///     <see cref="GovernanceResolutionDecision.ItemType" /> (raw string for dictionary entries, JSON string for compliance
///     keys, GUID format for ids).
/// </remarks>
public class GovernanceResolutionCandidate
{
    /// <summary>Pack that supplied this value.</summary>
    public Guid PolicyPackId
    {
        get;
        set;
    }

    /// <summary>Pack display name.</summary>
    public string PolicyPackName
    {
        get;
        set;
    } = null!;

    /// <summary>Published version used for this contribution.</summary>
    public string Version
    {
        get;
        set;
    } = null!;

    /// <summary>Assignment scope tier (<see cref="GovernanceScopeLevel" />).</summary>
    public string ScopeLevel
    {
        get;
        set;
    } = null!;

    /// <summary>Sort key from <see cref="EffectiveGovernanceResolver.GetPrecedenceRank" />.</summary>
    public int PrecedenceRank
    {
        get;
        set;
    }

    /// <summary><c>true</c> for the single winner among <see cref="GovernanceResolutionDecision.Candidates" />.</summary>
    public bool WasSelected
    {
        get;
        set;
    }

    /// <summary>Serialized value or identifier as appropriate for the item type.</summary>
    public string ValueJson
    {
        get;
        set;
    } = null!;

    /// <summary>Assignment row id (tie-breaker and correlation to persistence).</summary>
    public Guid AssignmentId
    {
        get;
        set;
    }

    /// <summary>Assignment timestamp (secondary sort after rank).</summary>
    public DateTime AssignedUtc
    {
        get;
        set;
    }
}
