namespace ArchiForge.Persistence.Replay;

/// <summary>
/// Service-layer input for <see cref="IAuthorityReplayService.ReplayAsync"/>.
/// </summary>
public class ReplayRequest
{
    /// <summary>Run to load and replay.</summary>
    public Guid RunId { get; set; }

    /// <summary>
    /// One of <see cref="ReplayMode.ReconstructOnly"/>, <see cref="ReplayMode.RebuildManifest"/>, or <see cref="ReplayMode.RebuildArtifacts"/> (case-insensitive match in the implementation).
    /// </summary>
    public string Mode { get; set; } = ReplayMode.ReconstructOnly;
}
