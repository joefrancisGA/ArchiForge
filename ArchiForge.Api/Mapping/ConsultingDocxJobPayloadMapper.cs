using ArchiForge.Api.Models;
using ArchiForge.Application.Jobs;

namespace ArchiForge.Api.Mapping;

internal static class ConsultingDocxJobPayloadMapper
{
    public static ConsultingDocxJobPayload ToPayload(string runId, ConsultingDocxExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ConsultingDocxJobPayload
        {
            RunId = runId,
            TemplateProfile = request.TemplateProfile,
            Audience = request.Audience,
            ExternalDelivery = request.ExternalDelivery,
            ExecutiveFriendly = request.ExecutiveFriendly,
            RegulatedEnvironment = request.RegulatedEnvironment,
            NeedDetailedEvidence = request.NeedDetailedEvidence,
            NeedExecutionTraces = request.NeedExecutionTraces,
            NeedDeterminismOrCompareAppendices = request.NeedDeterminismOrCompareAppendices,
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
}
