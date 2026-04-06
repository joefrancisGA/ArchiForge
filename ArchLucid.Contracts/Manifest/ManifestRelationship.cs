using System.ComponentModel.DataAnnotations;

using ArchiForge.Contracts.Common;

namespace ArchiForge.Contracts.Manifest;

/// <summary>
/// A directed edge in the architecture graph, representing a runtime dependency
/// between two components (services or datastores) in a <see cref="GoldenManifest"/>.
/// </summary>
public sealed class ManifestRelationship
{
    /// <summary>Unique relationship identifier.</summary>
    [Required]
    public string RelationshipId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// <see cref="ManifestService.ServiceId"/> or <see cref="ManifestDatastore.DatastoreId"/>
    /// of the source component (the caller/producer side of the dependency).
    /// </summary>
    [Required]
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// <see cref="ManifestService.ServiceId"/> or <see cref="ManifestDatastore.DatastoreId"/>
    /// of the target component (the callee/consumer side of the dependency).
    /// </summary>
    [Required]
    public string TargetId { get; set; } = string.Empty;

    /// <summary>Nature of the dependency (calls, reads from, writes to, publishes to, etc.).</summary>
    [Required]
    public RelationshipType RelationshipType { get; set; }

    /// <summary>Optional human-readable description of why this relationship exists.</summary>
    public string? Description { get; set; }
}
