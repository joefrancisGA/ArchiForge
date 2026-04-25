namespace ArchLucid.Persistence.Coordination.Replay;

/// <summary>
///     String tokens for <see cref="ReplayRequest.Mode" /> / API replay requests (matched case-insensitively in
///     <see cref="AuthorityReplayService" />).
/// </summary>
public static class ReplayMode
{
    /// <summary>Load run detail and validate only; no decision engine or persistence writes.</summary>
    public const string ReconstructOnly = "ReconstructOnly";

    /// <summary>Re-run decisioning from stored context/graph/findings; persist new trace and manifest.</summary>
    public const string RebuildManifest = "RebuildManifest";

    /// <summary><see cref="RebuildManifest" /> plus artifact synthesis and bundle persistence.</summary>
    public const string RebuildArtifacts = "RebuildArtifacts";
}
