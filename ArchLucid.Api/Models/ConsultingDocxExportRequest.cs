using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class ConsultingDocxExportRequest
{
    public string? TemplateProfile { get; set; }
    public string? Audience { get; set; }
    public bool ExternalDelivery { get; set; }
    public bool ExecutiveFriendly { get; set; }
    public bool RegulatedEnvironment { get; set; }
    public bool NeedDetailedEvidence { get; set; }
    public bool NeedExecutionTraces { get; set; }
    public bool NeedDeterminismOrCompareAppendices { get; set; }
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

