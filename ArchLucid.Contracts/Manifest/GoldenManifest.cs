using System.ComponentModel.DataAnnotations;

namespace ArchiForge.Contracts.Manifest;

/// <summary>
/// The authoritative resolved architecture manifest produced by a committed run.
/// Contains all services, datastores, relationships, governance metadata, and versioning
/// information that describe the target architecture state.
/// </summary>
public sealed class GoldenManifest
{
    /// <summary>Identifier of the run that produced this manifest.</summary>
    [Required]
    public string RunId { get; set; } = string.Empty;

    /// <summary>Name of the system described by this architecture manifest.</summary>
    [Required]
    public string SystemName { get; set; } = string.Empty;

    /// <summary>All resolved services included in the target architecture.</summary>
    [Required]
    public List<ManifestService> Services { get; set; } = [];

    /// <summary>All resolved datastores included in the target architecture.</summary>
    [Required]
    public List<ManifestDatastore> Datastores { get; set; } = [];

    /// <summary>All resolved inter-service and service-to-datastore relationships.</summary>
    [Required]
    public List<ManifestRelationship> Relationships { get; set; } = [];

    /// <summary>
    /// Governance metadata for this manifest including compliance tags, policy
    /// constraints, required controls, and risk classification.
    /// </summary>
    [Required]
    public ManifestGovernance Governance { get; set; } = new();

    /// <summary>
    /// Version, schema, and provenance metadata for this manifest snapshot.
    /// </summary>
    [Required]
    public ManifestMetadata Metadata { get; set; } = new();
}
