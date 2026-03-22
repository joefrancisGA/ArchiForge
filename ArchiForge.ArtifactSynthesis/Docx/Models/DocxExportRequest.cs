using ArchiForge.Core.Comparison;
using ArchiForge.Core.Explanation;

namespace ArchiForge.ArtifactSynthesis.Docx.Models;

public class DocxExportRequest
{
    public Guid RunId { get; set; }
    public Guid ManifestId { get; set; }

    public string DocumentTitle { get; set; } = "ArchiForge Architecture Package";
    public string Subtitle { get; set; } = "Generated Architecture Document";

    public bool IncludeArtifactsAppendix { get; set; } = true;
    public bool IncludeComplianceSection { get; set; } = true;
    public bool IncludeCoverageSection { get; set; } = true;
    public bool IncludeIssuesSection { get; set; } = true;

    /// <summary>Embeds a diagram image (v1: PNG placeholder; later Mermaid/graph render).</summary>
    public bool IncludeArchitectureDiagram { get; set; } = true;

    /// <summary>When set, appends an architecture comparison section (base = this export run).</summary>
    public ComparisonResult? ManifestComparison { get; set; }

    /// <summary>AI narrative when a manifest comparison is included.</summary>
    public ComparisonExplanationResult? ComparisonExplanation { get; set; }

    /// <summary>Optional AI narrative for the primary run (executive / stakeholder wording).</summary>
    public ExplanationResult? RunExplanation { get; set; }
}
