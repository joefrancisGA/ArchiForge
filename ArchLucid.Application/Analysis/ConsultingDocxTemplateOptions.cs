namespace ArchLucid.Application.Analysis;

/// <summary>
/// Configuration options that control the content and appearance of a consulting
/// architecture analysis Docx report.
/// </summary>
/// <remarks>
/// Bind these options from configuration using the standard ASP.NET Core
/// <c>IOptions&lt;ConsultingDocxTemplateOptions&gt;</c> pattern. Property defaults
/// represent the baseline client-delivery report style.
/// </remarks>
public sealed class ConsultingDocxTemplateOptions
{
    /// <summary>Human-readable display name for this set of options (e.g. <c>Client Delivery Report</c>).</summary>
    public string ProfileDisplayName { get; set; } = string.Empty;

    /// <summary>Short description of the profile's purpose shown in selection UIs.</summary>
    public string ProfileDescription { get; set; } = string.Empty;

    /// <summary>
    /// Free-text description of the profile's intended audience (e.g. <c>External clients</c>).
    /// </summary>
    public string IntendedAudience { get; set; } = string.Empty;

    /// <summary>
    /// Relative sort order when displaying this profile alongside alternatives. Lower values appear first.
    /// Defaults to <c>100</c>.
    /// </summary>
    public int DisplayOrder { get; set; } = 100;

    /// <summary>Organization name injected into report headers and the <see cref="GeneratedByLine"/>.</summary>
    public string OrganizationName { get; set; } = "ArchLucid";

    /// <summary>Main title displayed on the report cover page.</summary>
    public string DocumentTitle { get; set; } = "Architecture Analysis Report";

    /// <summary>
    /// Format string for the cover-page subtitle. Supports <c>{SystemName}</c> as a placeholder.
    /// </summary>
    public string SubtitleFormat { get; set; } = "{SystemName}";

    /// <summary>Footer line identifying the preparing organization.</summary>
    public string GeneratedByLine { get; set; } = "Prepared by ArchLucid";

    /// <summary>Primary brand colour used for headings, expressed as a 6-character hex string (no <c>#</c>).</summary>
    public string PrimaryColorHex { get; set; } = "2E4053";

    /// <summary>Secondary brand colour used for sub-headings and borders.</summary>
    public string SecondaryColorHex { get; set; } = "4F81BD";

    /// <summary>Light fill colour used for table row highlights and callout boxes.</summary>
    public string AccentFillHex { get; set; } = "EAF2F8";

    /// <summary>Primary body text colour.</summary>
    public string BodyColorHex { get; set; } = "1F1F1F";

    /// <summary>Subtle text colour used for captions and metadata.</summary>
    public string SubtleColorHex { get; set; } = "666666";

    /// <summary>When <see langword="true"/>, a document-control table is rendered after the cover page.</summary>
    public bool IncludeDocumentControl { get; set; } = true;

    /// <summary>When <see langword="true"/>, a table of contents is generated.</summary>
    public bool IncludeTableOfContents { get; set; } = true;

    /// <summary>When <see langword="true"/>, an executive summary section is rendered.</summary>
    public bool IncludeExecutiveSummary { get; set; } = true;

    /// <summary>When <see langword="true"/>, an architecture overview section is rendered.</summary>
    public bool IncludeArchitectureOverview { get; set; } = true;

    /// <summary>When <see langword="true"/>, the evidence and constraints section is included.</summary>
    public bool IncludeEvidenceAndConstraints { get; set; } = true;

    /// <summary>When <see langword="true"/>, detailed architecture component descriptions are included.</summary>
    public bool IncludeArchitectureDetails { get; set; } = true;

    /// <summary>When <see langword="true"/>, a governance and controls section is included.</summary>
    public bool IncludeGovernanceAndControls { get; set; } = true;

    /// <summary>When <see langword="true"/>, an AI explainability section is included.</summary>
    public bool IncludeExplainabilitySection { get; set; } = true;

    /// <summary>When <see langword="true"/>, a conclusions section is appended.</summary>
    public bool IncludeConclusions { get; set; } = true;

    /// <summary>When <see langword="true"/>, a Mermaid diagram appendix is rendered.</summary>
    public bool IncludeAppendixMermaid { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/>, an appendix listing per-agent execution trace summaries
    /// is included.
    /// </summary>
    public bool IncludeAppendixExecutionTraceIndex { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/>, a determinism check and run-comparison appendix is included.
    /// </summary>
    public bool IncludeAppendixDeterminismAndComparison { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/>, a logo image is embedded in the cover page header.
    /// Requires <see cref="LogoPath"/> to be set.
    /// </summary>
    public bool IncludeLogo { get; set; } = false;

    /// <summary>
    /// Absolute path to the logo image file. Only used when <see cref="IncludeLogo"/> is
    /// <see langword="true"/>.
    /// </summary>
    public string? LogoPath { get; set; }

    /// <summary>
    /// Template string for the executive-summary opening sentence.
    /// Supports <c>{SystemName}</c>, <c>{OrganizationName}</c>, <c>{ServiceCount}</c>,
    /// <c>{DatastoreCount}</c>, and <c>{ControlCount}</c> placeholders.
    /// </summary>
    public string ExecutiveSummaryTextTemplate { get; set; } =
        "{SystemName} was analyzed by {OrganizationName} and resolved into an architecture containing {ServiceCount} service(s), {DatastoreCount} datastore(s), and {ControlCount} required control(s).";

    /// <summary>Introductory paragraph for the architecture overview section.</summary>
    public string ArchitectureOverviewIntro { get; set; } =
        "The following section summarizes the resolved architecture and presents the primary runtime view.";

    /// <summary>Closing paragraph for the conclusions section.</summary>
    public string ConclusionsText { get; set; } =
        "The architecture analysis produced a resolved manifest and supporting explainability artifacts suitable for technical review.";
}
