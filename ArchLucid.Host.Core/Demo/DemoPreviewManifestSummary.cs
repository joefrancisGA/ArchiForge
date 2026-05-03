namespace ArchLucid.Host.Core.Demo;

/// <summary>Compact manifest summary for marketing preview (mirrors <c>ManifestSummaryResponse</c> fields).</summary>
public sealed class DemoPreviewManifestSummary
{
    public required string ManifestId
    {
        get;
        init;
    }

    public required string RunId
    {
        get;
        init;
    }

    public required DateTime CreatedUtc
    {
        get;
        init;
    }

    public required string ManifestHash
    {
        get;
        init;
    }

    public required string RuleSetId
    {
        get;
        init;
    }

    public required string RuleSetVersion
    {
        get;
        init;
    }

    public int DecisionCount
    {
        get;
        init;
    }

    public int WarningCount
    {
        get;
        init;
    }

    public int UnresolvedIssueCount
    {
        get;
        init;
    }

    public required string Status
    {
        get;
        init;
    }

    public bool HasWarnings
    {
        get;
        init;
    }

    public bool HasUnresolvedIssues
    {
        get;
        init;
    }

    public required string OperatorSummary
    {
        get;
        init;
    }
}
