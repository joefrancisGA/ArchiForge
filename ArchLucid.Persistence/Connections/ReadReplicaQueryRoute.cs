namespace ArchiForge.Persistence.Connections;

/// <summary>
/// Selects which <see cref="SqlReadReplicaSettings"/> key wins when multiple read-scale-out connection strings are configured.
/// </summary>
public enum ReadReplicaQueryRoute
{
    /// <summary>Heavy <c>dbo.Runs</c> list reads (project scope).</summary>
    AuthorityRunList,

    /// <summary>Policy pack assignment + pack + version reads on the governance-resolution path.</summary>
    GovernanceResolution,

    /// <summary><c>dbo.GoldenManifests</c> lookup by scope + manifest id.</summary>
    GoldenManifestLookup,
}
