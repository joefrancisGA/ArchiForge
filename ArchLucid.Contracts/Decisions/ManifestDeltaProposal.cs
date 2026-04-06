using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Contracts.Decisions;

/// <summary>
/// An agent's proposed additions to the <see cref="GoldenManifest"/> being synthesized.
/// Multiple proposals from different agents are merged by the manifest synthesis pipeline;
/// conflicting additions are resolved by priority and evaluation scores.
/// </summary>
public sealed class ManifestDeltaProposal
{
    /// <summary>Unique identifier for this proposal.</summary>
    public string ProposalId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>The agent type that submitted this proposal.</summary>
    public AgentType SourceAgent { get; set; }

    /// <summary>
    /// Services the proposing agent wants to add to the manifest topology.
    /// Duplicates (by service name) across proposals are deduplicated during merge.
    /// </summary>
    public List<ManifestService> AddedServices { get; set; } = [];

    /// <summary>
    /// Data stores the proposing agent wants to add to the manifest topology.
    /// Deduplicated by datastore name during merge.
    /// </summary>
    public List<ManifestDatastore> AddedDatastores { get; set; } = [];

    /// <summary>
    /// Relationships between services/datastores the proposing agent wants to add.
    /// Deduplicated by source/target pair during merge.
    /// </summary>
    public List<ManifestRelationship> AddedRelationships { get; set; } = [];

    /// <summary>
    /// Security controls that the proposing agent requires for the topology it proposed.
    /// These are merged into <see cref="GoldenManifest"/> required controls.
    /// </summary>
    public List<string> RequiredControls { get; set; } = [];

    /// <summary>
    /// Warnings the proposing agent wants surfaced in the manifest, such as
    /// unresolved capability gaps or missing policy coverage.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}
