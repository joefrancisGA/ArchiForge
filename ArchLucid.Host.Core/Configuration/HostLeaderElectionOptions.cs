using System.Diagnostics.CodeAnalysis;

namespace ArchLucid.Host.Core.Configuration;

/// <summary>
/// SQL row leases so only one replica runs advisory scan, data archival, and retrieval indexing outbox loops.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Configuration binding DTO with no logic.")]
public sealed class HostLeaderElectionOptions
{
    public const string SectionName = "HostLeaderElection";

    /// <summary>When false, hosted services run on every replica (legacy behavior).</summary>
    public bool Enabled
    {
        get;
        set;
    } = true;

    /// <summary>Lease TTL; renewed periodically while leader. Range enforced at startup.</summary>
    public int LeaseDurationSeconds
    {
        get;
        set;
    } = 90;

    /// <summary>How often the leader renews the lease (must be well under <see cref="LeaseDurationSeconds"/>).</summary>
    public int RenewIntervalSeconds
    {
        get;
        set;
    } = 25;

    /// <summary>How long followers sleep before retrying acquisition.</summary>
    public int FollowerPollMilliseconds
    {
        get;
        set;
    } = 2000;
}
