namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>
/// One piece of evidence tying a theme to a run, artifact coordinates, and/or a persisted feedback row.
/// At least one link target should be populated (validated by future services).
/// </summary>
public sealed class ImprovementThemeEvidence
{
    /// <summary>Correlation id for UI, exports, and deduplication.</summary>
    public Guid EvidenceId { get; init; }

    /// <summary>Parent theme.</summary>
    public Guid ThemeId { get; init; }

    /// <summary>Legacy architecture run id when the signal or rollup referenced a run.</summary>
    public string? ArchitectureRunId { get; init; }

    /// <summary>Optional authority bundle artifact (relational model).</summary>
    public Guid? AuthorityBundleId { get; init; }

    /// <summary>Sort order within <see cref="AuthorityBundleId"/> when set.</summary>
    public int? AuthorityArtifactSortOrder { get; init; }

    /// <summary>Free-text artifact hint from pilot feedback when bundle coordinates are unknown.</summary>
    public string? PilotArtifactHint { get; init; }

    /// <summary><c>ProductLearningPilotSignals.SignalId</c> when evidence is a concrete feedback row.</summary>
    public Guid? SignalId { get; init; }
}
