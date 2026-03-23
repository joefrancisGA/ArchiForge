namespace ArchiForge.Api.Contracts;

/// <summary>
/// JSON contract for run-to-run comparison (excludes embedded <see cref="ArchiForge.Persistence.Queries.RunSummaryDto"/> payloads).
/// </summary>
public class RunComparisonResponse
{
    /// <inheritdoc cref="ArchiForge.Persistence.Compare.RunComparisonResult.LeftRunId"/>
    public Guid LeftRunId
    {
        get; set;
    }

    /// <inheritdoc cref="ArchiForge.Persistence.Compare.RunComparisonResult.RightRunId"/>
    public Guid RightRunId
    {
        get; set;
    }

    /// <inheritdoc cref="ArchiForge.Persistence.Compare.RunComparisonResult.RunLevelDiffs"/>
    public List<DiffItemResponse> RunLevelDiffs { get; set; } = [];

    /// <inheritdoc cref="ArchiForge.Persistence.Compare.RunComparisonResult.ManifestComparison"/>
    public ManifestComparisonResponse? ManifestComparison
    {
        get; set;
    }
}
