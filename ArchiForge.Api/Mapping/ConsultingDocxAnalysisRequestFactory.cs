using ArchiForge.Api.Models;
using ArchiForge.Application.Analysis;

namespace ArchiForge.Api.Mapping;

/// <summary>Builds <see cref="ArchitectureAnalysisRequest"/> for consulting DOCX export from API models.</summary>
internal static class ConsultingDocxAnalysisRequestFactory
{
    public static ArchitectureAnalysisRequest Create(string runId, ConsultingDocxExportRequest request) =>
        new()
        {
            RunId = runId,
            IncludeEvidence = request.IncludeEvidence,
            IncludeExecutionTraces = request.IncludeExecutionTraces,
            IncludeManifest = request.IncludeManifest,
            IncludeDiagram = request.IncludeDiagram,
            // Consulting template options are currently configured globally via IOptions;
            // the API request influences the analysis content via the Include* flags.
            IncludeSummary = true,
            IncludeDeterminismCheck = request.IncludeDeterminismCheck,
            DeterminismIterations = request.DeterminismIterations,
            IncludeManifestCompare = request.IncludeManifestCompare,
            CompareManifestVersion = request.CompareManifestVersion,
            IncludeAgentResultCompare = request.IncludeAgentResultCompare,
            CompareRunId = request.CompareRunId
        };
}
