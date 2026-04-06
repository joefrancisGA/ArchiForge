namespace ArchiForge.Contracts.Metadata;

/// <summary>
/// Audit row for an export produced from a run (analysis markdown, DOCX, consulting profile, etc.), including template resolution and analysis options used.
/// </summary>
public sealed class RunExportRecord
{
    /// <summary>Primary key of the export record.</summary>
    public string ExportRecordId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Run that was exported.</summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>Pipeline-specific type key (e.g. analysis-report-docx).</summary>
    public string ExportType { get; set; } = string.Empty;

    /// <summary>File or payload format (e.g. markdown, docx).</summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>Download filename or object key hint.</summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>Consulting DOCX profile key when applicable.</summary>
    public string? TemplateProfile { get; set; }

    /// <summary>Display name for <see cref="TemplateProfile"/>.</summary>
    public string? TemplateProfileDisplayName { get; set; }

    /// <summary><see langword="true"/> when the profile was auto-selected from recommendations.</summary>
    public bool WasAutoSelected { get; set; }

    /// <summary>Reason string from profile resolution.</summary>
    public string? ResolutionReason { get; set; }

    /// <summary>Manifest version at export time when relevant.</summary>
    public string? ManifestVersion { get; set; }

    /// <summary>Free-text operator notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Serialized analysis request body used for the export, when captured.</summary>
    public string? AnalysisRequestJson { get; set; }

    /// <summary>Whether the export included the evidence section.</summary>
    public bool? IncludedEvidence { get; set; }

    /// <summary>Whether execution traces were included.</summary>
    public bool? IncludedExecutionTraces { get; set; }

    /// <summary>Whether manifest content was included.</summary>
    public bool? IncludedManifest { get; set; }

    /// <summary>Whether diagram output was included.</summary>
    public bool? IncludedDiagram { get; set; }

    /// <summary>Whether architecture summary was included.</summary>
    public bool? IncludedSummary { get; set; }

    /// <summary>Whether determinism appendix was included.</summary>
    public bool? IncludedDeterminismCheck { get; set; }

    /// <summary>Iteration count for determinism when included.</summary>
    public int? DeterminismIterations { get; set; }

    /// <summary>Whether manifest compare appendix was included.</summary>
    public bool? IncludedManifestCompare { get; set; }

    /// <summary>Baseline manifest version for compare when included.</summary>
    public string? CompareManifestVersion { get; set; }

    /// <summary>Whether agent-result compare appendix was included.</summary>
    public bool? IncludedAgentResultCompare { get; set; }

    /// <summary>Peer run id for agent-result compare when included.</summary>
    public string? CompareRunId { get; set; }

    /// <summary>UTC creation time.</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

