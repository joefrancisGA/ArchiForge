using ArchiForge.Application.Analysis;

namespace ArchiForge.Application.Jobs;

/// <summary>
/// Serializable export parameters for async analysis-report DOCX jobs (no preloaded run detail).
/// </summary>
public sealed record AnalysisReportDocxJobPayload
{
    public string RunId { get; init; } = string.Empty;

    public bool IncludeEvidence { get; init; } = true;

    public bool IncludeExecutionTraces { get; init; } = true;

    public bool IncludeManifest { get; init; } = true;

    public bool IncludeDiagram { get; init; } = true;

    public bool IncludeSummary { get; init; } = true;

    public bool IncludeDeterminismCheck { get; init; }

    public int DeterminismIterations { get; init; } = 3;

    public bool IncludeManifestCompare { get; init; }

    public string? CompareManifestVersion { get; init; }

    public bool IncludeAgentResultCompare { get; init; }

    public string? CompareRunId { get; init; }

    public static AnalysisReportDocxJobPayload FromAnalysisRequest(ArchitectureAnalysisRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new AnalysisReportDocxJobPayload
        {
            RunId = request.RunId,
            IncludeEvidence = request.IncludeEvidence,
            IncludeExecutionTraces = request.IncludeExecutionTraces,
            IncludeManifest = request.IncludeManifest,
            IncludeDiagram = request.IncludeDiagram,
            IncludeSummary = request.IncludeSummary,
            IncludeDeterminismCheck = request.IncludeDeterminismCheck,
            DeterminismIterations = request.DeterminismIterations,
            IncludeManifestCompare = request.IncludeManifestCompare,
            CompareManifestVersion = request.CompareManifestVersion,
            IncludeAgentResultCompare = request.IncludeAgentResultCompare,
            CompareRunId = request.CompareRunId
        };
    }

    public ArchitectureAnalysisRequest ToAnalysisRequest() =>
        new()
        {
            RunId = RunId,
            IncludeEvidence = IncludeEvidence,
            IncludeExecutionTraces = IncludeExecutionTraces,
            IncludeManifest = IncludeManifest,
            IncludeDiagram = IncludeDiagram,
            IncludeSummary = IncludeSummary,
            IncludeDeterminismCheck = IncludeDeterminismCheck,
            DeterminismIterations = DeterminismIterations,
            IncludeManifestCompare = IncludeManifestCompare,
            CompareManifestVersion = CompareManifestVersion,
            IncludeAgentResultCompare = IncludeAgentResultCompare,
            CompareRunId = CompareRunId
        };
}
