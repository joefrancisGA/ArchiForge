namespace ArchiForge.Contracts.Governance.Preview;

/// <summary>
/// Request payload for the governance environment comparison endpoint; identifies the two environment slots
/// whose currently active manifests should be diffed.
/// </summary>
public sealed class GovernanceEnvironmentComparisonRequest
{
    /// <summary>Source environment slot (e.g. <c>dev</c>).</summary>
    public string SourceEnvironment { get; set; } = "dev";

    /// <summary>Target environment slot (e.g. <c>test</c>).</summary>
    public string TargetEnvironment { get; set; } = "test";
}
