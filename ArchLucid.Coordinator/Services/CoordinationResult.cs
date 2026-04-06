using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Coordinator.Services;

/// <summary>
/// Result of a <see cref="ICoordinatorService.CreateRunAsync"/> call.
/// When <see cref="Success"/> is <see langword="true"/>, <see cref="Run"/>, <see cref="EvidenceBundle"/>,
/// and <see cref="Tasks"/> are populated and ready for persistence.
/// When <see langword="false"/>, only <see cref="Errors"/> is meaningful.
/// </summary>
public sealed class CoordinationResult
{
    /// <summary>
    /// The architecture run record created by the coordinator.
    /// Only meaningful when <see cref="Success"/> is <see langword="true"/>.
    /// </summary>
    public ArchitectureRun Run { get; set; } = new();

    /// <summary>
    /// The evidence bundle assembled from the architecture request.
    /// Only meaningful when <see cref="Success"/> is <see langword="true"/>.
    /// </summary>
    public EvidenceBundle EvidenceBundle { get; set; } = new();

    /// <summary>
    /// The starter agent tasks generated for the run (one per required agent type).
    /// Only meaningful when <see cref="Success"/> is <see langword="true"/>.
    /// </summary>
    public List<AgentTask> Tasks { get; set; } = [];

    /// <summary>
    /// Non-fatal warnings (currently unused; reserved for future advisory signals).
    /// </summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// Validation or orchestration errors that prevented a successful run creation.
    /// Non-empty implies <see cref="Success"/> is <see langword="false"/>.
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// <see langword="true"/> when <see cref="Errors"/> is empty and coordination completed successfully.
    /// </summary>
    public bool Success => Errors.Count == 0;
}
