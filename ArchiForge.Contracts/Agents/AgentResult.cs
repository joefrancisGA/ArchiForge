using System.ComponentModel.DataAnnotations;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Findings;

namespace ArchiForge.Contracts.Agents;

public sealed class AgentResult
{
    [Required]
    public string ResultId { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public string TaskId { get; set; } = string.Empty;

    [Required]
    public string RunId { get; set; } = string.Empty;

    [Required]
    public AgentType AgentType { get; set; }

    [Required]
    public List<string> Claims { get; set; } = [];

    [Required]
    public List<string> EvidenceRefs { get; set; } = [];

    [Range(0.0, 1.0)]
    public double Confidence { get; set; }

    public List<ArchitectureFinding> Findings { get; set; } = [];

    public ManifestDeltaProposal? ProposedChanges { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}