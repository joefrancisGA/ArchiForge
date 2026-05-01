namespace ArchLucid.Contracts.Evolution;

/// <summary>
///     Read-only architecture analysis flags for a simulation pass. Does not enable determinism checks or replay (those
///     mutate or spawn runs).
/// </summary>
public sealed class SimulationReadProfile : IEquatable<SimulationReadProfile>
{
    /// <summary>Default 60R shadow profile: manifest + summary only, no evidence, traces, diagram, or diffs.</summary>
    public static SimulationReadProfile StrictReadOnly
    {
        get;
    } = new();

    public bool IncludeEvidence
    {
        get;
        init;
    }

    public bool IncludeExecutionTraces
    {
        get;
        init;
    }

    public bool IncludeManifest
    {
        get;
        init;
    } = true;

    public bool IncludeDiagram
    {
        get;
        init;
    }

    public bool IncludeSummary
    {
        get;
        init;
    } = true;

    public bool IncludeManifestCompare
    {
        get;
        init;
    }

    public string? CompareManifestVersion
    {
        get;
        init;
    }

    public bool IncludeAgentResultCompare
    {
        get;
        init;
    }

    public string? CompareRunId
    {
        get;
        init;
    }

    /// <inheritdoc />
    public bool Equals(SimulationReadProfile? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return IncludeEvidence == other.IncludeEvidence &&
               IncludeExecutionTraces == other.IncludeExecutionTraces &&
               IncludeManifest == other.IncludeManifest &&
               IncludeDiagram == other.IncludeDiagram &&
               IncludeSummary == other.IncludeSummary &&
               IncludeManifestCompare == other.IncludeManifestCompare &&
               IncludeAgentResultCompare == other.IncludeAgentResultCompare &&
               string.Equals(CompareManifestVersion, other.CompareManifestVersion, StringComparison.Ordinal) &&
               string.Equals(CompareRunId, other.CompareRunId, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is SimulationReadProfile other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(IncludeEvidence);
        hash.Add(IncludeExecutionTraces);
        hash.Add(IncludeManifest);
        hash.Add(IncludeDiagram);
        hash.Add(IncludeSummary);
        hash.Add(IncludeManifestCompare);
        hash.Add(IncludeAgentResultCompare);
        hash.Add(CompareManifestVersion, StringComparer.Ordinal);
        hash.Add(CompareRunId, StringComparer.Ordinal);

        return hash.ToHashCode();
    }
}
