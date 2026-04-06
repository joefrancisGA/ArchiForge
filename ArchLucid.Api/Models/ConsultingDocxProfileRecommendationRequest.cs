using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class ConsultingDocxProfileRecommendationRequest
{
    public string? Audience { get; set; }
    public bool ExternalDelivery { get; set; }
    public bool ExecutiveFriendly { get; set; }
    public bool RegulatedEnvironment { get; set; }
    public bool NeedDetailedEvidence { get; set; }
    public bool NeedExecutionTraces { get; set; }
    public bool NeedDeterminismOrCompareAppendices { get; set; }
}

