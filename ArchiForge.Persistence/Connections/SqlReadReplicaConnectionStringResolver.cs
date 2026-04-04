namespace ArchiForge.Persistence.Connections;

/// <summary>
/// Resolves the failover-group read-only listener (or legacy per-route override) for scoped read paths.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SqlReadReplicaSettings.FailoverGroupReadOnlyListenerConnectionString"/> is the Azure SQL failover group
/// <strong>secondary read-only</strong> endpoint. When both it and <see cref="SqlReadReplicaSettings.AuthorityRunListReadsConnectionString"/>
/// are set, run lists prefer the legacy key (backward compatibility); governance and manifest lookups prefer the failover listener first.
/// </para>
/// </remarks>
public static class SqlReadReplicaConnectionStringResolver
{
    /// <summary>
    /// Returns a trimmed connection string to open directly, or <see langword="null"/> to use the primary resilient factory.
    /// </summary>
    public static string? Resolve(ReadReplicaQueryRoute route, SqlReadReplicaSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        string? failover = settings.FailoverGroupReadOnlyListenerConnectionString?.Trim();
        string? runList = settings.AuthorityRunListReadsConnectionString?.Trim();

        return route switch
        {
            ReadReplicaQueryRoute.AuthorityRunList => string.IsNullOrEmpty(runList) ? NullIfEmpty(failover) : runList,

            ReadReplicaQueryRoute.GovernanceResolution =>
                string.IsNullOrEmpty(failover) ? NullIfEmpty(runList) : failover,

            ReadReplicaQueryRoute.GoldenManifestLookup =>
                string.IsNullOrEmpty(failover) ? NullIfEmpty(runList) : failover,

            _ => throw new ArgumentOutOfRangeException(nameof(route), route, null),
        };
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrEmpty(value) ? null : value;
}
