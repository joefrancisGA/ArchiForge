namespace ArchLucid.Persistence.Coordination.Replay;

/// <summary>
///     Flags and narrative notes produced during <see cref="AuthorityReplayService.ReplayAsync" />.
/// </summary>
public class ReplayValidationResult
{
    /// <summary><see langword="true" /> when the original run had a context snapshot row.</summary>
    public bool ContextPresent
    {
        get;
        set;
    }

    /// <summary><see langword="true" /> when the original run had a graph snapshot.</summary>
    public bool GraphPresent
    {
        get;
        set;
    }

    /// <summary><see langword="true" /> when the original run had a findings snapshot.</summary>
    public bool FindingsPresent
    {
        get;
        set;
    }

    /// <summary><see langword="true" /> when the original run had a golden manifest.</summary>
    public bool ManifestPresent
    {
        get;
        set;
    }

    /// <summary><see langword="true" /> when the original run had a decision trace.</summary>
    public bool TracePresent
    {
        get;
        set;
    }

    /// <summary><see langword="true" /> when the original run had a non-empty artifact bundle reference.</summary>
    public bool ArtifactsPresent
    {
        get;
        set;
    }

    /// <summary>
    ///     When a manifest exists, <see langword="true" /> if recomputed hash matches
    ///     <see cref="ArchLucid.Decisioning.Models.GoldenManifest.ManifestHash" /> on the stored manifest.
    /// </summary>
    public bool ManifestHashMatches
    {
        get;
        set;
    }

    /// <summary><see langword="true" /> after artifact synthesis when the rebuilt bundle contains artifacts.</summary>
    public bool ArtifactBundlePresentAfterReplay
    {
        get;
        set;
    }

    /// <summary>Operator-facing explanation (modes, missing data, hash parity, artifact counts).</summary>
    public List<string> Notes
    {
        get;
        set;
    } = [];
}
