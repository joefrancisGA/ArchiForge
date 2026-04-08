using ArchLucid.Core.Comparison;
using ArchLucid.Core.Explanation;
using ArchLucid.Decisioning.Models;

namespace ArchLucid.ArtifactSynthesis.Docx.Models;

/// <summary>
/// Parameters controlling DOCX section inclusion and optional AI comparison/run narratives for an architecture package export.
/// </summary>
public class DocxExportRequest
{
    /// <summary>Primary run id (metadata lines).</summary>
    public Guid RunId { get; set; }

    /// <summary>Manifest id (metadata lines).</summary>
    public Guid ManifestId { get; set; }

    /// <summary>Cover / title page heading.</summary>
    public string DocumentTitle { get; set; } = "ArchLucid Architecture Package";

    /// <summary>Subtitle under the title.</summary>
    public string Subtitle { get; set; } = "Generated Architecture Document";

    /// <summary>When <see langword="true"/>, emits synthesized artifact appendix content.</summary>
    public bool IncludeArtifactsAppendix { get; set; } = true;

    /// <summary>When <see langword="true"/>, emits compliance section blocks.</summary>
    public bool IncludeComplianceSection { get; set; } = true;

    /// <summary>When <see langword="true"/>, emits requirements coverage section.</summary>
    public bool IncludeCoverageSection { get; set; } = true;

    /// <summary>When <see langword="true"/>, emits unresolved issues section.</summary>
    public bool IncludeIssuesSection { get; set; } = true;

    /// <summary>Embeds a diagram image (v1: PNG placeholder; later Mermaid/graph render).</summary>
    public bool IncludeArchitectureDiagram { get; set; } = true;

    /// <summary>When set, appends an architecture comparison section (base = this export run).</summary>
    public ComparisonResult? ManifestComparison { get; set; }

    /// <summary>AI narrative when a manifest comparison is included.</summary>
    public ComparisonExplanationResult? ComparisonExplanation { get; set; }

    /// <summary>Optional AI narrative for the primary run (executive / stakeholder wording).</summary>
    public ExplanationResult? RunExplanation { get; set; }

    /// <summary>When null, DOCX export synthesizes an empty findings snapshot for advisory only.</summary>
    public FindingsSnapshot? FindingsSnapshot { get; set; }

    /// <summary>Builds the request used by <c>GET api/docx/runs/.../architecture-package</c>.</summary>
    /// <param name="runId">Exported run.</param>
    /// <param name="manifestId">Manifest tied to the run.</param>
    /// <param name="documentTitle">DOCX title.</param>
    /// <param name="subtitle">DOCX subtitle.</param>
    /// <param name="manifestComparison">Optional base→target comparison when user requested a compare run.</param>
    /// <param name="comparisonExplanation">Optional LLM narrative for the comparison.</param>
    /// <param name="runExplanation">Optional LLM narrative for the primary run.</param>
    /// <param name="findingsSnapshot">Persisted findings when available.</param>
    public static DocxExportRequest ForArchitecturePackage(
        Guid runId,
        Guid manifestId,
        string documentTitle,
        string subtitle,
        ComparisonResult? manifestComparison,
        ComparisonExplanationResult? comparisonExplanation,
        ExplanationResult? runExplanation,
        FindingsSnapshot? findingsSnapshot) =>
        new()
        {
            RunId = runId,
            ManifestId = manifestId,
            DocumentTitle = documentTitle,
            Subtitle = subtitle,
            ManifestComparison = manifestComparison,
            ComparisonExplanation = comparisonExplanation,
            RunExplanation = runExplanation,
            FindingsSnapshot = findingsSnapshot,
        };
}
