using ArchiForge.Persistence.Queries;

namespace ArchiForge.Persistence.Compare;

public class RunComparisonResult
{
    public Guid LeftRunId { get; set; }
    public Guid RightRunId { get; set; }

    public RunSummaryDto? LeftRun { get; set; }
    public RunSummaryDto? RightRun { get; set; }

    public ManifestComparisonResult? ManifestComparison { get; set; }

    public List<DiffItem> RunLevelDiffs { get; set; } = [];
}
