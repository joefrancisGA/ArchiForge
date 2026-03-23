using ArchiForge.Persistence.Queries;

namespace ArchiForge.Persistence.Compare;

/// <summary>
/// Outcome of <see cref="IAuthorityCompareService.CompareRunsAsync"/> including optional nested manifest diff.
/// </summary>
/// <remarks>
/// <see cref="LeftRun"/> and <see cref="RightRun"/> are populated for service consumers; the HTTP API maps to <c>RunComparisonResponse</c> without embedding full summaries.
/// </remarks>
public class RunComparisonResult
{
    public Guid LeftRunId
    {
        get; set;
    }

    public Guid RightRunId
    {
        get; set;
    }

    /// <summary>Summaries loaded for the comparison (not always exposed on the wire).</summary>
    public RunSummaryDto? LeftRun
    {
        get; set;
    }

    /// <summary>Right-hand run summary (same semantics as <see cref="LeftRun"/>).</summary>
    public RunSummaryDto? RightRun
    {
        get; set;
    }

    /// <summary>Present when both runs have golden manifest ids and manifest comparison succeeds.</summary>
    public ManifestComparisonResult? ManifestComparison
    {
        get; set;
    }

    /// <summary>High-level run field changes (e.g. project slug, description).</summary>
    public List<DiffItem> RunLevelDiffs { get; set; } = [];
}
