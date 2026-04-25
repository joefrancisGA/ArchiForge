namespace ArchLucid.Persistence.Queries;

/// <summary>
///     Compact golden-manifest projection (counts and rule-set metadata) for dashboards without loading full
///     <see cref="ArchLucid.Decisioning.Models.GoldenManifest" /> JSON.
/// </summary>
/// <remarks>HTTP mapping: <c>ArchLucid.Api.Contracts.ManifestSummaryResponse</c>.</remarks>
public class ManifestSummaryDto
{
    public Guid ManifestId
    {
        get;
        set;
    }

    public Guid RunId
    {
        get;
        set;
    }

    public DateTime CreatedUtc
    {
        get;
        set;
    }

    public string ManifestHash
    {
        get;
        set;
    } = null!;

    public string RuleSetId
    {
        get;
        set;
    } = null!;

    public string RuleSetVersion
    {
        get;
        set;
    } = null!;

    /// <summary>Number of captured decisions on the manifest.</summary>
    public int DecisionCount
    {
        get;
        set;
    }

    /// <summary>Number of warnings on the manifest.</summary>
    public int WarningCount
    {
        get;
        set;
    }

    /// <summary>Count of unresolved issue items.</summary>
    public int UnresolvedIssueCount
    {
        get;
        set;
    }

    /// <summary>High-level status from manifest metadata (e.g. evaluation outcome label).</summary>
    public string Status
    {
        get;
        set;
    } = null!;
}
