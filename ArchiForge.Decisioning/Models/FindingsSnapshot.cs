namespace ArchiForge.Decisioning.Models;

public class FindingsSnapshot
{
    /// <summary>Snapshot container schema version.</summary>
    public int SchemaVersion { get; set; } = FindingsSchema.CurrentSnapshotVersion;

    public Guid FindingsSnapshotId { get; set; }
    public Guid RunId { get; set; }
    public Guid ContextSnapshotId { get; set; }
    public Guid GraphSnapshotId { get; set; }
    public DateTime CreatedUtc { get; set; }

    public List<Finding> Findings { get; set; } = [];

    public IReadOnlyList<Finding> GetByCategory(string category)
        => Findings
            .Where(f => string.Equals(f.Category, category, StringComparison.OrdinalIgnoreCase))
            .ToList();

    public IReadOnlyList<Finding> GetByType(string findingType)
        => Findings
            .Where(f => string.Equals(f.FindingType, findingType, StringComparison.OrdinalIgnoreCase))
            .ToList();
}

