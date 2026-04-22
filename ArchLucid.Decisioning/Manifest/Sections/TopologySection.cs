using ArchLucid.Contracts.Manifest;

namespace ArchLucid.Decisioning.Manifest.Sections;

public class TopologySection
{
    public List<string> SelectedPatterns { get; set; } = [];
    public List<string> Resources { get; set; } = [];
    public List<string> Gaps { get; set; } = [];

    /// <summary>PR A0.5 — typed service nodes; persisted in <c>TopologyJson</c> with the rest of topology.</summary>
    public List<ManifestService> Services { get; set; } = [];

    /// <summary>PR A0.5 — typed datastore nodes; persisted in <c>TopologyJson</c>.</summary>
    public List<ManifestDatastore> Datastores { get; set; } = [];
}

