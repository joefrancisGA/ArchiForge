using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Contracts.Architecture;

/// <summary>
/// Canonical aggregate for a single architecture run: the authoritative source for
/// export, compare, governance, and explanation features. Assembled by
/// <c>IRunDetailQueryService</c> — do not rebuild this by hand in controllers.
/// </summary>
public sealed class ArchitectureRunDetail
{
    /// <summary>Core run record including status, timestamps, and version references.</summary>
    public ArchitectureRun Run { get; set; } = new();

    /// <summary>Agent tasks created for this run.</summary>
    public List<AgentTask> Tasks { get; set; } = [];

    /// <summary>Agent results produced during execution.</summary>
    public List<AgentResult> Results { get; set; } = [];

    /// <summary>
    /// Golden manifest produced during commit, or <see langword="null"/> when the run
    /// has not yet been committed or the manifest could not be loaded.
    /// </summary>
    public GoldenManifest? Manifest { get; set; }

    /// <summary>Decision traces recorded during commit; empty before commit.</summary>
    public List<DecisionTrace> DecisionTraces { get; set; } = [];

    /// <summary>Convenience accessor: <see langword="true"/> when the run has a committed manifest.</summary>
    public bool IsCommitted => Manifest is not null;
}
