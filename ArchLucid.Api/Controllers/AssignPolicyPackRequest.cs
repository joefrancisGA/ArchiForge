namespace ArchiForge.Api.Controllers;

/// <summary>Body for <c>POST …/policy-packs/{id}/assign</c>.</summary>
/// <remarks>
/// <see cref="ScopeLevel"/> must be <c>Tenant</c>, <c>Workspace</c>, or <c>Project</c> (case-insensitive); default <c>Project</c>.
/// </remarks>
public sealed class AssignPolicyPackRequest
{
    /// <summary>Must match an existing <c>PolicyPackVersions</c> row for the pack.</summary>
    public string Version { get; set; } = null!;

    /// <summary>Governance tier for the assignment; stored on <c>PolicyPackAssignments.ScopeLevel</c>.</summary>
    public string ScopeLevel { get; set; } = "Project";

    /// <summary>When true, increases precedence within the same tier during resolution.</summary>
    public bool IsPinned { get; set; }
}
