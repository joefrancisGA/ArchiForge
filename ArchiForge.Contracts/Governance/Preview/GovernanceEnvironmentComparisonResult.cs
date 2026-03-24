namespace ArchiForge.Contracts.Governance.Preview;

public sealed class GovernanceEnvironmentComparisonResult
{
    public string SourceEnvironment { get; set; } = string.Empty;
    public string TargetEnvironment { get; set; } = string.Empty;
    public List<GovernanceDiffItem> Differences { get; set; } = [];
    public List<string> Notes { get; set; } = [];
}
