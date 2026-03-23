using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Application.Analysis;

public sealed class ArchitectureAnalysisRequest
{
    public string RunId { get; set; } = string.Empty;

    /// <summary>
    /// When set by the caller (e.g. a controller that already loaded the run for a 404 guard),
    /// <see cref="IArchitectureAnalysisService.BuildAsync"/> uses this instance directly and
    /// skips the redundant <c>GetByIdAsync</c> round-trip.
    /// </summary>
    public ArchitectureRun? PreloadedRun { get; set; }

    public bool IncludeEvidence { get; set; } = true;

    public bool IncludeExecutionTraces { get; set; } = true;

    public bool IncludeManifest { get; set; } = true;

    public bool IncludeDiagram { get; set; } = true;

    public bool IncludeSummary { get; set; } = true;

    public bool IncludeDeterminismCheck { get; set; } = false;

    public int DeterminismIterations { get; set; } = 3;

    public bool IncludeManifestCompare { get; set; } = false;

    public string? CompareManifestVersion
    {
        get; set;
    }

    public bool IncludeAgentResultCompare { get; set; } = false;

    public string? CompareRunId
    {
        get; set;
    }
}
