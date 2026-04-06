using System.ComponentModel.DataAnnotations;

namespace ArchiForge.Contracts.Agents;

/// <summary>
/// The evidence context assembled for an architecture run and passed to every agent.
/// Contains the request description, applicable policies, service catalog hints,
/// architectural pattern hints, prior manifest context, and advisory notes.
/// </summary>
public sealed class AgentEvidencePackage
{
    /// <summary>Unique evidence package identifier, generated at creation time.</summary>
    public string EvidencePackageId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Identifier of the run this evidence package was built for.</summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>Identifier of the originating <c>ArchitectureRequest</c>.</summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>Name of the system being analysed.</summary>
    public string SystemName { get; set; } = string.Empty;

    /// <summary>Target deployment environment (e.g. <c>prod</c>, <c>staging</c>).</summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>Primary cloud provider for the target architecture (e.g. <c>Azure</c>).</summary>
    public string CloudProvider { get; set; } = string.Empty;

    /// <summary>Request description, constraints, capabilities, and assumptions.</summary>
    [Required]
    public RequestEvidence Request { get; set; } = new();

    /// <summary>Applicable policy evidence entries injected for agent evaluation.</summary>
    public List<PolicyEvidence> Policies { get; set; } = [];

    /// <summary>Service catalog entries relevant to the request context.</summary>
    public List<ServiceCatalogEvidence> ServiceCatalog { get; set; } = [];

    /// <summary>Architecture pattern hints relevant to the request.</summary>
    public List<PatternEvidence> Patterns { get; set; } = [];

    /// <summary>
    /// Summary of the most recently committed manifest for this system, provided
    /// when <c>PriorManifestVersion</c> is set on the request.
    /// <see langword="null"/> for greenfield runs or when hydration is not yet implemented.
    /// </summary>
    public PriorManifestEvidence? PriorManifest { get; set; }

    /// <summary>Advisory notes added by the evidence builder (execution mode, hydration warnings, pattern hints, etc.).</summary>
    public List<EvidenceNote> Notes { get; set; } = [];

    /// <summary>UTC timestamp when this package was built.</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
