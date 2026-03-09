using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Contracts.Decisions;

public sealed class ManifestDeltaProposal
{
    public string ProposalId { get; set; } = Guid.NewGuid().ToString("N");

    public AgentType SourceAgent { get; set; }

    public List<ManifestService> AddedServices { get; set; } = [];

    public List<ManifestDatastore> AddedDatastores { get; set; } = [];

    public List<ManifestRelationship> AddedRelationships { get; set; } = [];

    public List<string> RequiredControls { get; set; } = [];

    public List<string> Warnings { get; set; } = [];
}