namespace ArchiForge.Contracts.Governance.Preview;

/// <summary>
/// Request payload for the governance preview endpoint; identifies the candidate run, target manifest version,
/// and the environment slot to compare the candidate against.
/// </summary>
public sealed class GovernancePreviewRequest
{
    /// <summary>Id of the architecture run to preview for promotion.</summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>Manifest version within the run to preview.</summary>
    public string ManifestVersion { get; set; } = string.Empty;

    /// <summary>Target environment slot (e.g. <c>dev</c>, <c>test</c>, <c>prod</c>).</summary>
    public string Environment { get; set; } = "dev";
}
