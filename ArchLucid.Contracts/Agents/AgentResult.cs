using System.ComponentModel.DataAnnotations;

using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Findings;

namespace ArchiForge.Contracts.Agents;

/// <summary>
/// The output produced by an agent after completing its assigned <see cref="AgentTask"/>.
/// Contains claims, evidence references, findings, proposed manifest changes, and
/// a confidence score used by the decision engine during manifest synthesis.
/// </summary>
public sealed class AgentResult
{
    /// <summary>Unique result identifier, generated at creation time.</summary>
    [Required]
    public string ResultId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Identifier of the <see cref="AgentTask"/> this result fulfils.</summary>
    [Required]
    public string TaskId { get; set; } = string.Empty;

    /// <summary>Run identifier shared with the originating task.</summary>
    [Required]
    public string RunId { get; set; } = string.Empty;

    /// <summary>Type of agent that produced this result.</summary>
    [Required]
    public AgentType AgentType { get; set; }

    /// <summary>
    /// Natural-language claims produced by the agent supporting its proposed architecture changes.
    /// </summary>
    [Required]
    public List<string> Claims { get; set; } = [];

    /// <summary>
    /// References to evidence items (policy IDs, service catalog IDs, pattern IDs, etc.) that
    /// the agent used to justify its claims.
    /// </summary>
    [Required]
    public List<string> EvidenceRefs { get; set; } = [];

    /// <summary>
    /// Agent's self-reported confidence in its result, in the range [0.0, 1.0].
    /// The decision engine weights this when resolving conflicting proposals.
    /// </summary>
    [Range(0.0, 1.0)]
    public double Confidence { get; set; }

    /// <summary>Architecture findings identified by the agent (security gaps, topology issues, etc.).</summary>
    public List<ArchitectureFinding> Findings { get; set; } = [];

    /// <summary>
    /// Proposed additions and removals to the golden manifest.
    /// <see langword="null"/> when the agent has no structural proposals (e.g. evaluation-only agents).
    /// </summary>
    public ManifestDeltaProposal? ProposedChanges { get; set; }

    /// <summary>UTC timestamp when this result was created.</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
