namespace ArchiForge.Decisioning.Governance.Resolution;

/// <summary>
/// DTO-style snapshot of how an assignment would participate in precedence ordering (for future APIs, UI, or exports).
/// </summary>
/// <remarks>
/// Not used by <see cref="EffectiveGovernanceResolver"/> merge logic today; reserved for explicit precedence inspection without full resolution.
/// </remarks>
public class PolicyAssignmentPrecedence
{
    /// <summary><see cref="PolicyPacks.PolicyPackAssignment.AssignmentId"/>.</summary>
    public Guid AssignmentId { get; set; }

    /// <summary><see cref="GovernanceScopeLevel"/> string.</summary>
    public string ScopeLevel { get; set; } = null!;

    /// <summary>Numeric rank comparable across assignments (<see cref="EffectiveGovernanceResolver.GetPrecedenceRank"/>).</summary>
    public int PrecedenceRank { get; set; }

    /// <summary>Whether the assignment was pinned.</summary>
    public bool IsPinned { get; set; }

    /// <summary><see cref="PolicyPacks.PolicyPackAssignment.AssignedUtc"/>.</summary>
    public DateTime AssignedUtc { get; set; }
}
