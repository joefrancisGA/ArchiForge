using System.ComponentModel.DataAnnotations;

using ArchLucid.Contracts.Common;

namespace ArchLucid.Contracts.Metadata;

/// <summary>
///     Represents a single architecture analysis run and tracks its lifecycle from creation
///     through agent execution to manifest commitment.
/// </summary>
/// <remarks>
///     A run is created by <c>ArchitectureRunService.CreateRunAsync</c>, executed by
///     <c>ExecuteRunAsync</c>, and committed to a <see cref="ArchLucid.Contracts.Manifest.GoldenManifest" />
///     by <c>CommitRunAsync</c>. Status transitions follow:
///     <c>Created → ReadyForCommit → Committed</c> (or <c>Failed</c> on error).
/// </remarks>
public sealed class ArchitectureRun
{
    /// <summary>Unique identifier for this run, formatted as a lowercase 32-character hex string.</summary>
    [Required]
    public string RunId
    {
        get;
        set;
    } = Guid.NewGuid().ToString("N");

    /// <summary>Identifier of the <c>ArchitectureRequest</c> that initiated this run.</summary>
    [Required]
    public string RequestId
    {
        get;
        set;
    } = string.Empty;

    /// <summary>Current lifecycle status of the run.</summary>
    [Required]
    public ArchitectureRunStatus Status
    {
        get;
        set;
    } = ArchitectureRunStatus.Created;

    /// <summary>UTC timestamp when the run record was created.</summary>
    [Required]
    public DateTime CreatedUtc
    {
        get;
        set;
    } = DateTime.UtcNow;

    /// <summary>
    ///     UTC timestamp when the run reached a terminal state (<c>Committed</c> or <c>Failed</c>).
    ///     <see langword="null" /> while the run is still in progress.
    /// </summary>
    public DateTime? CompletedUtc
    {
        get;
        set;
    }

    /// <summary>
    ///     Version string of the most recently committed <c>GoldenManifest</c> for this run
    ///     (e.g. <c>v1</c>, <c>v2</c>). <see langword="null" /> until the run is committed.
    /// </summary>
    public string? CurrentManifestVersion
    {
        get;
        set;
    }

    /// <summary>Context snapshot ID created during run creation (nullable for older runs).</summary>
    public string? ContextSnapshotId
    {
        get;
        set;
    }

    /// <summary>Graph snapshot ID created from the context snapshot (nullable for older runs).</summary>
    public Guid? GraphSnapshotId
    {
        get;
        set;
    }

    /// <summary>Findings snapshot ID created from the graph snapshot (nullable for older runs).</summary>
    public Guid? FindingsSnapshotId
    {
        get;
        set;
    }

    /// <summary>Golden manifest ID created by the decision engine (nullable for older runs).</summary>
    public Guid? GoldenManifestId
    {
        get;
        set;
    }

    /// <summary>Decision trace ID created by the decision engine (nullable for older runs).</summary>
    public Guid? DecisionTraceId
    {
        get;
        set;
    }

    /// <summary>Artifact bundle ID produced after golden manifest synthesis (nullable for older runs).</summary>
    public Guid? ArtifactBundleId
    {
        get;
        set;
    }

    /// <summary>
    ///     OpenTelemetry W3C trace ID persisted at run creation (<c>Activity.TraceId</c>); nullable for older runs.
    ///     Distinct from the per-request trace on API responses.
    /// </summary>
    public string? OtelTraceId
    {
        get;
        set;
    }

    /// <summary>
    ///     Ordered list of agent task identifiers associated with this run.
    ///     Populated during run creation and used to track execution progress.
    /// </summary>
    public IReadOnlyList<string> TaskIds
    {
        get;
        set;
    } = [];

    /// <summary>
    ///     When <see langword="true" />, real-mode pilot execution fell back to deterministic simulator output (
    ///     <c>archlucid try --real</c> path).
    /// </summary>
    public bool RealModeFellBackToSimulator
    {
        get;
        set;
    }

    /// <summary>Azure OpenAI deployment name captured when simulator fallback was recorded (nullable).</summary>
    public string? PilotAoaiDeploymentSnapshot
    {
        get;
        set;
    }
}
