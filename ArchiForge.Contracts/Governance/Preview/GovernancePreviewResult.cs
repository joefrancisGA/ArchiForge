namespace ArchiForge.Contracts.Governance.Preview;

public sealed class GovernancePreviewResult
{
    public string Environment { get; set; } = string.Empty;

    public string? CurrentRunId { get; set; }
    public string? CurrentManifestVersion { get; set; }

    public string PreviewRunId { get; set; } = string.Empty;
    public string PreviewManifestVersion { get; set; } = string.Empty;

    public List<GovernanceDiffItem> Differences { get; set; } = [];
    public List<string> Notes { get; set; } = [];
}
