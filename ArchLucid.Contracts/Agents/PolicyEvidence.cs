namespace ArchiForge.Contracts.Agents;

/// <summary>
/// Represents a single governance policy entry included in an <see cref="AgentEvidencePackage"/>.
/// Agents use this information to evaluate compliance and apply required security controls.
/// Well-known <see cref="PolicyId"/> values are defined in
/// <c>ArchiForge.Application.Evidence.BuiltInPolicyIds</c>.
/// </summary>
public sealed class PolicyEvidence
{
    /// <summary>
    /// Stable identifier for the policy. Must not change between builds;
    /// agents may reference this value in their reasoning output.
    /// </summary>
    public string PolicyId { get; set; } = string.Empty;

    /// <summary>Short display name for the policy.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Concise description of the policy's intent and scope.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Mandatory security controls that compliant architectures must implement
    /// in order to satisfy this policy.
    /// </summary>
    public List<string> RequiredControls { get; set; } = [];

    /// <summary>Classification tags for filtering and grouping (e.g., <c>security</c>, <c>networking</c>).</summary>
    public List<string> Tags { get; set; } = [];
}
