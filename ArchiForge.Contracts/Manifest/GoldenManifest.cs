using System.ComponentModel.DataAnnotations;

namespace ArchiForge.Contracts.Manifest;

public sealed class GoldenManifest
{
    [Required]
    public string RunId { get; set; } = string.Empty;

    [Required]
    public string SystemName { get; set; } = string.Empty;

    [Required]
    public List<ManifestService> Services { get; set; } = [];

    [Required]
    public List<ManifestDatastore> Datastores { get; set; } = [];

    [Required]
    public List<ManifestRelationship> Relationships { get; set; } = [];

    [Required]
    public ManifestGovernance Governance { get; set; } = new();

    [Required]
    public ManifestMetadata Metadata { get; set; } = new();
}