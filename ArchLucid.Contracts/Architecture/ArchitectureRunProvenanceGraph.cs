using System.ComponentModel.DataAnnotations;

namespace ArchiForge.Contracts.Architecture;

/// <summary>
/// Linkage graph and timeline for a coordinator architecture run: request, tasks, results, findings,
/// committed manifest, decision traces, and materialized decision nodes.
/// </summary>
public sealed class ArchitectureRunProvenanceGraph
{
    /// <summary>Architecture run identifier (hex string).</summary>
    [Required]
    public string RunId { get; set; } = string.Empty;

    /// <summary>Structural nodes (requests, tasks, snapshots, manifest, traces, etc.).</summary>
    public List<ArchitectureLinkageNode> Nodes { get; set; } = [];

    /// <summary>Directed edges describing provenance and containment.</summary>
    public List<ArchitectureLinkageEdge> Edges { get; set; } = [];

    /// <summary>
    /// Chronological trace events for operator review (subset of graph, sorted ascending by time).
    /// </summary>
    public List<ArchitectureTraceTimelineEntry> Timeline { get; set; } = [];

    /// <summary>
    /// Automated integrity findings (e.g. manifest metadata missing trace ids). Empty when no issues detected.
    /// </summary>
    public List<string> TraceabilityGaps { get; set; } = [];
}

/// <summary>One vertex in <see cref="ArchitectureRunProvenanceGraph"/>.</summary>
public sealed class ArchitectureLinkageNode
{
    /// <summary>Stable id within this graph (e.g. <c>run:abc...</c>).</summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>Domain type; use constants on <see cref="ArchitectureLinkageKinds.Nodes"/>.</summary>
    [Required]
    public string Type { get; set; } = string.Empty;

    /// <summary>External reference (task id, trace id, request id, etc.).</summary>
    [Required]
    public string ReferenceId { get; set; } = string.Empty;

    /// <summary>Short display label.</summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional facets (agent type, event type, snapshot hints).</summary>
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Directed link between <see cref="ArchitectureLinkageNode"/> instances.</summary>
public sealed class ArchitectureLinkageEdge
{
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>Use constants on <see cref="ArchitectureLinkageKinds.Edges"/>.</summary>
    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public string FromNodeId { get; set; } = string.Empty;

    [Required]
    public string ToNodeId { get; set; } = string.Empty;

    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>One row in the run trace timeline.</summary>
public sealed class ArchitectureTraceTimelineEntry
{
    public DateTime TimestampUtc { get; set; }

    /// <summary>Use constants on <see cref="ArchitectureLinkageKinds.Timeline"/>.</summary>
    [Required]
    public string Kind { get; set; } = string.Empty;

    [Required]
    public string Label { get; set; } = string.Empty;

    public string? ReferenceId { get; set; }

    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
