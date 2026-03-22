using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ContextIngestion.Models;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.Persistence.Models;

namespace ArchiForge.Persistence.Queries;

public class RunDetailDto
{
    public RunRecord Run { get; set; } = null!;
    public ContextSnapshot? ContextSnapshot { get; set; }
    public GraphSnapshot? GraphSnapshot { get; set; }
    public FindingsSnapshot? FindingsSnapshot { get; set; }
    public DecisionTrace? DecisionTrace { get; set; }
    public GoldenManifest? GoldenManifest { get; set; }
    public ArtifactBundle? ArtifactBundle { get; set; }
}
