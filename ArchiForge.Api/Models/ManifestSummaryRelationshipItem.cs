namespace ArchiForge.Api.Models;

/// <summary>Relationship entry within a <see cref="ManifestSummaryJsonResponse"/>.</summary>
public sealed class ManifestSummaryRelationshipItem
{
    public string SourceId { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string RelationshipType { get; set; } = string.Empty;

    public string? Description { get; set; }
}
