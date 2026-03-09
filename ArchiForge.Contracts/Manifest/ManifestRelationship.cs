using System.ComponentModel.DataAnnotations;
using ArchiForge.Contracts.Common;

namespace ArchiForge.Contracts.Manifest;

public sealed class ManifestRelationship
{
    [Required]
    public string RelationshipId { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public string SourceId { get; set; } = string.Empty;

    [Required]
    public string TargetId { get; set; } = string.Empty;

    [Required]
    public RelationshipType RelationshipType { get; set; }

    public string? Description { get; set; }
}