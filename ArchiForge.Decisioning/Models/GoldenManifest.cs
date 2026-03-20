using ArchiForge.Decisioning.Manifest.Sections;

namespace ArchiForge.Decisioning.Models;

public class GoldenManifest
{
    public Guid ManifestId { get; set; }
    public Guid RunId { get; set; }
    public Guid ContextSnapshotId { get; set; }
    public Guid GraphSnapshotId { get; set; }
    public Guid FindingsSnapshotId { get; set; }
    public Guid DecisionTraceId { get; set; }

    public DateTime CreatedUtc { get; set; }

    public string ManifestHash { get; set; } = null!;

    public string RuleSetId { get; set; } = null!;
    public string RuleSetVersion { get; set; } = null!;
    public string RuleSetHash { get; set; } = null!;

    public ManifestMetadata Metadata { get; set; } = new();
    public RequirementsCoverageSection Requirements { get; set; } = new();
    public TopologySection Topology { get; set; } = new();
    public SecuritySection Security { get; set; } = new();
    public ComplianceSection Compliance { get; set; } = new();
    public CostSection Cost { get; set; } = new();
    public ConstraintSection Constraints { get; set; } = new();
    public UnresolvedIssuesSection UnresolvedIssues { get; set; } = new();

    public List<ResolvedArchitectureDecision> Decisions { get; set; } = [];
    public List<string> Assumptions { get; set; } = [];
    public List<string> Warnings { get; set; } = [];

    public ManifestProvenance Provenance { get; set; } = new();
}

