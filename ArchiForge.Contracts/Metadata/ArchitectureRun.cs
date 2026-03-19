using System.ComponentModel.DataAnnotations;
using ArchiForge.Contracts.Common;

namespace ArchiForge.Contracts.Metadata;

public sealed class ArchitectureRun
{
    [Required]
    public string RunId { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public string RequestId { get; set; } = string.Empty;

    [Required]
    public ArchitectureRunStatus Status { get; set; } = ArchitectureRunStatus.Created;

    [Required]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedUtc { get; set; }

    public string? CurrentManifestVersion { get; set; }

    /// <summary>Context snapshot ID created during run creation (nullable for older runs).</summary>
    public string? ContextSnapshotId { get; set; }

    /// <summary>Graph snapshot ID created from the context snapshot (nullable for older runs).</summary>
    public Guid? GraphSnapshotId { get; set; }

    /// <summary>Findings snapshot ID created from the graph snapshot (nullable for older runs).</summary>
    public Guid? FindingsSnapshotId { get; set; }

    /// <summary>Golden manifest ID created by the decision engine (nullable for older runs).</summary>
    public Guid? GoldenManifestId { get; set; }

    /// <summary>Decision trace ID created by the decision engine (nullable for older runs).</summary>
    public Guid? DecisionTraceId { get; set; }

    /// <summary>Artifact bundle ID produced after golden manifest synthesis (nullable for older runs).</summary>
    public Guid? ArtifactBundleId { get; set; }

    public List<string> TaskIds { get; set; } = [];
}