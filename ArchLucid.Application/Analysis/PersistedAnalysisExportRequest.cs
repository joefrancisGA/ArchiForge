namespace ArchiForge.Application.Analysis;

/// <summary>
/// Persisted parameters for a consulting architecture analysis Docx export request.
/// </summary>
/// <remarks>
/// This record is stored alongside the export artifact so that the export can be
/// reproduced or audited later with the same settings.
/// <para>
/// Two overlapping groups of flags exist: <em>profile recommendation signals</em>
/// (<see cref="ExternalDelivery"/>, <see cref="ExecutiveFriendly"/>, etc.) feed
/// <see cref="IConsultingDocxTemplateRecommendationService"/> when no explicit
/// <see cref="TemplateProfile"/> is supplied; <em>section inclusion flags</em>
/// (<see cref="IncludeEvidence"/>, <see cref="IncludeExecutionTraces"/>, etc.) directly
/// gate individual report sections. Both sets may be set independently.
/// </para>
/// </remarks>
public sealed class PersistedAnalysisExportRequest
{
    /// <summary>
    /// Explicit template profile name (e.g. <c>executive</c>, <c>regulated</c>).
    /// When set, profile auto-recommendation is skipped.
    /// See <see cref="ConsultingDocxProfiles"/> for well-known values.
    /// </summary>
    public string? TemplateProfile { get; set; }

    // ── Profile recommendation signals ────────────────────────────────────────

    /// <summary>
    /// Free-text audience hint forwarded to the recommendation service when
    /// <see cref="TemplateProfile"/> is not set (e.g. <c>executive</c>, <c>compliance</c>).
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// <see langword="true"/> when the document will be delivered to an external client.
    /// Signals the <c>client</c> profile when no stronger flag is set.
    /// </summary>
    public bool ExternalDelivery { get; set; }

    /// <summary>
    /// <see langword="true"/> when the primary reader expects a concise non-technical brief.
    /// Signals the <c>executive</c> profile when no stronger flag is set.
    /// </summary>
    public bool ExecutiveFriendly { get; set; }

    /// <summary>
    /// <see langword="true"/> when the report must satisfy a regulated or compliance review.
    /// Signals the <c>regulated</c> profile — the highest-priority recommendation signal.
    /// </summary>
    public bool RegulatedEnvironment { get; set; }

    /// <summary>
    /// <see langword="true"/> to signal that a detailed evidence appendix is required.
    /// Promotes the <c>internal</c> profile when present without a higher-priority flag.
    /// </summary>
    public bool NeedDetailedEvidence { get; set; }

    /// <summary>
    /// <see langword="true"/> to signal that per-agent execution traces are required.
    /// Promotes the <c>internal</c> profile when present without a higher-priority flag.
    /// </summary>
    public bool NeedExecutionTraces { get; set; }

    /// <summary>
    /// <see langword="true"/> to signal that determinism-check or run-comparison appendices
    /// are required. Promotes the <c>internal</c> profile when present without a higher-priority flag.
    /// </summary>
    public bool NeedDeterminismOrCompareAppendices { get; set; }

    // ── Section inclusion flags ────────────────────────────────────────────────

    /// <summary>When <see langword="true"/>, an evidence bundle section is included in the report.</summary>
    public bool IncludeEvidence { get; set; }

    /// <summary>When <see langword="true"/>, a per-agent execution trace index is included.</summary>
    public bool IncludeExecutionTraces { get; set; }

    /// <summary>When <see langword="true"/>, the resolved manifest is included as an appendix.</summary>
    public bool IncludeManifest { get; set; }

    /// <summary>When <see langword="true"/>, a Mermaid architecture diagram is embedded.</summary>
    public bool IncludeDiagram { get; set; }

    /// <summary>When <see langword="true"/>, a run summary section is included.</summary>
    public bool IncludeSummary { get; set; }

    /// <summary>When <see langword="true"/>, a determinism-check appendix is generated.</summary>
    public bool IncludeDeterminismCheck { get; set; }

    /// <summary>
    /// Number of replay iterations used for the determinism check.
    /// Only meaningful when <see cref="IncludeDeterminismCheck"/> is <see langword="true"/>.
    /// </summary>
    public int DeterminismIterations { get; set; }

    /// <summary>
    /// When <see langword="true"/>, a manifest-diff comparison appendix is generated.
    /// Requires <see cref="CompareManifestVersion"/> to be set.
    /// </summary>
    public bool IncludeManifestCompare { get; set; }

    /// <summary>
    /// Manifest version string to compare the current run against.
    /// Only used when <see cref="IncludeManifestCompare"/> is <see langword="true"/>.
    /// </summary>
    public string? CompareManifestVersion { get; set; }

    /// <summary>
    /// When <see langword="true"/>, an agent-result diff comparison appendix is generated.
    /// Requires <see cref="CompareRunId"/> to be set.
    /// </summary>
    public bool IncludeAgentResultCompare { get; set; }

    /// <summary>
    /// Run identifier to compare agent results against.
    /// Only used when <see cref="IncludeAgentResultCompare"/> is <see langword="true"/>.
    /// </summary>
    public string? CompareRunId { get; set; }
}
