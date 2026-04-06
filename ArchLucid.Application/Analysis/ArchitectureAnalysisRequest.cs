using ArchiForge.Contracts.Architecture;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Application.Analysis;

public sealed class ArchitectureAnalysisRequest
{
    public string RunId { get; set; } = string.Empty;

    /// <summary>
    /// When set by the caller (e.g. after <see cref="IRunDetailQueryService.GetRunDetailAsync"/>),
    /// <see cref="IArchitectureAnalysisService.BuildAsync"/> uses this canonical aggregate and skips
    /// a second <see cref="IRunDetailQueryService.GetRunDetailAsync"/> call. Primary manifest and
    /// agent results are taken from this detail when present.
    /// </summary>
    /// <remarks>
    /// <see cref="ArchitectureRunDetail.Run"/>.<see cref="ArchitectureRun.RunId"/> must match <see cref="RunId"/>.
    /// </remarks>
    public ArchitectureRunDetail? PreloadedRunDetail { get; set; }

    /// <summary>
    /// Legacy preload: run metadata only. When <see cref="PreloadedRunDetail"/> is set, it takes precedence.
    /// Otherwise <see cref="IArchitectureAnalysisService.BuildAsync"/> may still call
    /// <see cref="IRunDetailQueryService.GetRunDetailAsync"/> to load tasks/results/manifest.
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
    public string? CompareManifestVersion { get; set; }
    public bool IncludeAgentResultCompare { get; set; } = false;
    public string? CompareRunId { get; set; }
}
