namespace ArchiForge.Application.Diffs;

public sealed class RelationshipDiffItem
{
    public string SourceId { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string RelationshipType { get; set; } = string.Empty;

    public string? Description { get; set; }
}
