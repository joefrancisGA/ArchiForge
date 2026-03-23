namespace ArchiForge.Api.Controllers;

/// <summary>Body for <c>POST …/policy-packs</c> (create pack + initial draft version).</summary>
/// <remarks>Validated by FluentValidation; <see cref="PackType"/> must be a known <c>PolicyPackType</c> string.</remarks>
public sealed class CreatePolicyPackRequest
{
    /// <summary>Display name (required).</summary>
    public string Name { get; set; } = null!;

    /// <summary>Optional description.</summary>
    public string Description { get; set; } = "";

    /// <summary>E.g. <c>ProjectCustom</c>, <c>TenantCustom</c>.</summary>
    public string PackType { get; set; } = null!;

    /// <summary>JSON object matching <c>PolicyPackContentDocument</c> shape for version <c>1.0.0</c> draft.</summary>
    public string InitialContentJson { get; set; } = "{}";
}

/// <summary>Body for <c>POST …/policy-packs/{id}/publish</c>.</summary>
/// <remarks><see cref="Version"/> must satisfy SemVer rules enforced by validator.</remarks>
public sealed class PublishPolicyPackVersionRequest
{
    /// <summary>Version label to publish or update in place.</summary>
    public string Version { get; set; } = null!;

    /// <summary>Full pack content JSON.</summary>
    public string ContentJson { get; set; } = "{}";
}

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
