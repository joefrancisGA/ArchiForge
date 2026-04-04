namespace ArchiForge.Data.Repositories;

/// <summary>Read model for <c>dbo.HostLeaderLeases</c> (operator diagnostics).</summary>
public sealed class HostLeaderLeaseSnapshot
{
    /// <summary>Stable lease key (e.g. hosted service name).</summary>
    public string LeaseName { get; init; } = "";

    /// <summary>Instance identifier holding the lease.</summary>
    public string HolderInstanceId { get; init; } = "";

    /// <summary>UTC expiry of the current lease.</summary>
    public DateTime LeaseExpiresUtc { get; init; }
}
