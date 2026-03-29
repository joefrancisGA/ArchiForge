using ArchiForge.Api.Models;
using ArchiForge.Application.Analysis;

using AppConsultingDocxProfileRecommendationRequest = ArchiForge.Application.Analysis.ConsultingDocxProfileRecommendationRequest;

namespace ArchiForge.Api.Mapping;

/// <summary>Maps <see cref="ConsultingDocxExportRequest"/> into audit / profile-resolution models.</summary>
internal static class ConsultingDocxExportAuditMapper
{
    /// <summary>Signals for <see cref="IConsultingDocxExportProfileSelector.Resolve"/>.</summary>
    public static AppConsultingDocxProfileRecommendationRequest ToRecommendationRequest(ConsultingDocxExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new AppConsultingDocxProfileRecommendationRequest
        {
            Audience = request.Audience,
            ExternalDelivery = request.ExternalDelivery,
            ExecutiveFriendly = request.ExecutiveFriendly,
            RegulatedEnvironment = request.RegulatedEnvironment,
            NeedDetailedEvidence = request.NeedDetailedEvidence,
            NeedExecutionTraces = request.NeedExecutionTraces,
            NeedDeterminismOrCompareAppendices = request.NeedDeterminismOrCompareAppendices
        };
    }

    /// <summary>
    /// Persisted shape for <see cref="IRunExportAuditService.RecordAsync"/>; <c>IncludeSummary</c> is always <see langword="true"/>
    /// to match <c>DownloadConsultingDocx</c> (analysis always includes the summary section).
    /// </summary>
    public static PersistedAnalysisExportRequest ToPersistedRequest(ConsultingDocxExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new PersistedAnalysisExportRequest
        {
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
            IncludeSummary = true,
            IncludeDeterminismCheck = request.IncludeDeterminismCheck,
            DeterminismIterations = request.DeterminismIterations,
            IncludeManifestCompare = request.IncludeManifestCompare,
            CompareManifestVersion = request.CompareManifestVersion,
            IncludeAgentResultCompare = request.IncludeAgentResultCompare,
            CompareRunId = request.CompareRunId
        };
    }
}
