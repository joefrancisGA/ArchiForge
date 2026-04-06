namespace ArchiForge.Contracts.Agents;

/// <summary>
/// Captures the complete set of reference material assembled for an architecture run —
/// policy references, service-catalog entries, and prior-manifest pointers — that is
/// provided as context to all agents during task execution.
/// </summary>
public sealed class EvidenceBundle
{
    /// <summary>Unique identifier for this evidence bundle.</summary>
    public string EvidenceBundleId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Human-readable description of the architecture request this bundle was built for.</summary>
    public string RequestDescription { get; set; } = string.Empty;

    /// <summary>Identifiers or URIs of governance policies included as evidence.</summary>
    public List<string> PolicyRefs { get; set; } = [];

    /// <summary>Identifiers or URIs of service-catalog entries included as evidence.</summary>
    public List<string> ServiceCatalogRefs { get; set; } = [];

    /// <summary>Manifest version strings or IDs of prior manifests included for context.</summary>
    public List<string> PriorManifestRefs { get; set; } = [];

    /// <summary>Arbitrary key/value pairs for additional evidence provenance or tagging.</summary>
    public Dictionary<string, string> Metadata { get; set; } = [];
}
