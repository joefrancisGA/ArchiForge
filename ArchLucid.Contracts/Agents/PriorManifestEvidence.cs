using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Contracts.Agents;

/// <summary>
/// A summary of a previously approved <c>GoldenManifest</c> included as context in an
/// <see cref="AgentEvidencePackage"/>. Agents use this to reason about existing topology,
/// avoid re-introducing already-present services, and preserve established security controls.
/// </summary>
/// <remarks>
/// When no prior manifest is available (e.g., greenfield runs), the evidence package will
/// not include a <see cref="PriorManifestEvidence"/> entry; an <see cref="EvidenceNote"/>
/// with type <c>PriorManifestUnavailable</c> will be added instead.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Contract DTO; no business logic.")]
public sealed class PriorManifestEvidence
{
    /// <summary>Version label of the prior manifest this evidence was extracted from.</summary>
    public string ManifestVersion { get; set; } = string.Empty;

    /// <summary>Short description of the prior architecture to orient agents quickly.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Service names already present in the prior manifest's topology.</summary>
    public List<string> ExistingServices { get; set; } = [];

    /// <summary>Datastore names already present in the prior manifest's topology.</summary>
    public List<string> ExistingDatastores { get; set; } = [];

    /// <summary>Security controls that were required by the prior manifest and should be preserved.</summary>
    public List<string> ExistingRequiredControls { get; set; } = [];
}
